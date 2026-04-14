using Microsoft.EntityFrameworkCore;
using RestApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IO;
using System;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Diagnostics;

using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Formatting.Elasticsearch;

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    // Konsola yaz
    .WriteTo.Console()
    // Elasticsearch'e yaz
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
        new Uri("http://localhost:9200"))
    {
        // Her gün yeni index oluştur
        IndexFormat = $"restapi-logs-{DateTime.UtcNow:yyyy.MM.dd}",
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
        // JSON formatı
        CustomFormatter = new ElasticsearchJsonFormatter(),
        // Batch ayarları
        BatchPostingLimit = 50,
        Period = TimeSpan.FromSeconds(2),
        // Hata durumunda dosyaya yaz
        BufferBaseFilename = "./logs/elastic-buffer",
        BufferFileSizeLimitBytes = 5242880, // 5MB
    })
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog'i kullan
builder.Host.UseSerilog();

// Servis adını tanımla
var serviceName = "RestApi";
var serviceVersion = "1.0.0";
// Resource tanımı (tüm telemetri verisine eklenir)
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion);
    // ===== TRACES =====
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("RestApi.ProductController")  // Manuel source kaydı
            .AddAspNetCoreInstrumentation()
            .SetResourceBuilder(resourceBuilder)
            // Otomatik Instrumentation
            .AddAspNetCoreInstrumentation(options =>
            {
                // Health check endpoint'lerini filtrele
                options.Filter = (httpContext) =>
                    !httpContext.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation()
            // OTLP Exporter (Collector'a gönder)
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:14317");
            });
    })
    // ===== METRICS =====
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            // Prometheus endpoint
            .AddPrometheusExporter(options =>
            {
                options.ScrapeEndpointPath = "/metrics";
            });
    });
    // ===== LOGS =====
builder.Logging.AddOpenTelemetry(logging =>
{
    logging
        .SetResourceBuilder(resourceBuilder)
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:14317");
        });
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
// Register PasswordManager to read configuration via DI
builder.Services.AddSingleton<RestApi.Services.PasswordManager>();

var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key");
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
}
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("X-Access-Token"))
                {
                    context.Token = context.Request.Cookies["X-Access-Token"];
                }
                return Task.CompletedTask;
            }
        };
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
    
var app = builder.Build();

// Request loglama middleware
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
    };
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<TraceIdMiddleware>(); 
// app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<GlobalMiddleware>();

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

 app.Run();
