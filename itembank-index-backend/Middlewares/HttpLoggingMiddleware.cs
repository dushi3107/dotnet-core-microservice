using System.Diagnostics;
using itembank_index_backend.Utils;

namespace itembank_index_backend.Middlewares;

public class HttpLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpLoggingMiddleware> _logger;

    public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var request = context.Request;
            var response = context.Response;
            var sourceHost = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(sourceHost))
            {
                sourceHost = context.Connection.RemoteIpAddress?.ToString();
            }

            var logMessage = $"[{Time.GetNowDateTimeString()}] " +
                             $"[SourceHost: {sourceHost}] " +
                             $"[Method: {request.Method}] " +
                             $"[Path: {request.Path}] " +
                             $"[Status: {response.StatusCode}] " +
                             $"[Duration: {stopwatch.ElapsedMilliseconds}ms]";

            _logger.LogInformation(logMessage);
        }
    }
}