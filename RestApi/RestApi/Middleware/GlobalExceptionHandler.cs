
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        // 1. Response'un gönderilip gönderilmediğini KONTROL ET
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("Response zaten başladığı için hata detayı yazılamadı.");
            return;
        }

        try
        {
            _logger.LogError(exception, "Unhandled exception...");

            var response = new
            {
                error = "An unexpected error occurred.",
                code = Guid.NewGuid().ToString()
            };

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(response);
        }
        catch (Exception logEx)
        {
            // Hata yakalayıcının hata fırlatmasını engelle!
            // En kötü senaryoda sessizce çık veya çok temel bir log at.
        }
    }
}