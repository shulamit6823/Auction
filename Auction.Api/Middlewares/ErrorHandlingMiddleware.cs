using Microsoft.AspNetCore.Mvc;

namespace Auction.Api.Middlewares;

// Catches any exception that no Controller handled, logs it, and returns a clean 500
// in the same ProblemDetails format as all our other errors - never a stack trace.
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected - not a server error, and there is no one to answer.
            _logger.LogInformation("Request was cancelled by the client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            // Part of the response was already sent - it cannot be changed anymore.
            if (context.Response.HasStarted)
                throw;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "שגיאת שרת",
                Detail = "אירעה שגיאה לא צפויה. נסו שוב מאוחר יותר."
            };
            // Clear() removes anything the failed code already set - including all headers.
            context.Response.Clear();

            if (context.Items.TryGetValue(RequestLoggingMiddleware.RequestIdItemKey, out var requestId))
            {
                problem.Extensions["requestId"] = requestId;
                context.Response.Headers[RequestLoggingMiddleware.RequestIdHeader] = requestId?.ToString();
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
