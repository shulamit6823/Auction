using System.Diagnostics;

namespace Auction.Api.Middlewares;

// Runs on every request. Tracks the request from the moment it enters the server
// until the response leaves it.
public class RequestLoggingMiddleware
{
    public const string RequestIdHeader = "X-Request-Id";
    public const string RequestIdItemKey = "RequestId";

    // The next step in the pipeline (the next middleware, or in the end the Controller).
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // A new id for every request - always created by the server, never taken from the client.
        var requestId = Guid.NewGuid().ToString();
        context.Items[RequestIdItemKey] = requestId;      // for code later in this request
        context.Response.Headers[RequestIdHeader] = requestId; // so the client can report it

        // Every log line written during this request (also in Controllers/Services) carries the id.
        using (_logger.BeginScope(new Dictionary<string, object> { [RequestIdItemKey] = requestId }))
        {
            // Before next: the request has arrived and has not reached the Controller yet.
            // Only method and path - never the body (it may contain a password).
            _logger.LogInformation("Request {RequestId} started: {Method} {Path}",
                requestId, context.Request.Method, context.Request.Path);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            catch
            {
                // After next - the request failed. The exception itself is logged by the
                // error handling middleware; here we only record that it ended and how long it took.
                _logger.LogWarning("Request {RequestId} failed with an exception after {ElapsedMs} ms",
                    requestId, stopwatch.ElapsedMilliseconds);
                throw;
            }

            // After next - the Controller has finished and the response is on its way back.
            _logger.LogInformation("Request {RequestId} finished: {StatusCode} in {ElapsedMs} ms",
                requestId, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
