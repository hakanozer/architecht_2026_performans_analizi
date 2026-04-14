using System.Diagnostics;

public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var activity = Activity.Current;

            if (activity != null)
            {
                context.Response.Headers["X-Trace-Id"] = activity.TraceId.ToString();
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}