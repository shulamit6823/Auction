namespace Auction.Api.Middlewares;

// Lets Program.cs write app.UseRequestLogging() instead of app.UseMiddleware<RequestLoggingMiddleware>().
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
