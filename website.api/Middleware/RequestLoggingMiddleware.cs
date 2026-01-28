using System.Text;

namespace website.api.Middleware;

/// <summary>
/// Comprehensive request logging middleware for MCP debugging
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log for API endpoints to reduce noise
        // AND exclude the status bridge polling which happens every few seconds
        if (context.Request.Path.StartsWithSegments("/api") && 
            !context.Request.Path.StartsWithSegments("/api/mcp/status/bridge"))
        {
            _logger.LogInformation("REQ {Method} {Path}", context.Request.Method, context.Request.Path);
            
            await _next(context);
            
            _logger.LogInformation("RES {Method} {Path} returned {StatusCode}", 
                context.Request.Method, 
                context.Request.Path, 
                context.Response.StatusCode);
        }
        else
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to register the request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}