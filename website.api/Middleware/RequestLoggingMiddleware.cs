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
        var isMcpOrOAuth = context.Request.Path.StartsWithSegments("/api/mcp") ||
                           context.Request.Path.StartsWithSegments("/api/oauth");
        
        var isSse = context.Request.Path.Value?.Contains("/sse") == true;

        // Log request info regardless
        if (isMcpOrOAuth)
        {
            await LogRequestAsync(context);
        }

        // For SSE, we MUST NOT buffer the response body, or the stream will never reach the client
        if (isMcpOrOAuth && !isSse)
        {
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            await LogResponseAsync(context, responseBody);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        else
        {
            // Just pass through for SSE or non-MCP requests
            await _next(context);
        }
    }

    private async Task LogRequestAsync(HttpContext context)
    {
        _logger.LogInformation("=== INCOMING REQUEST ===");
        _logger.LogInformation("Timestamp: {Timestamp}", DateTime.UtcNow);
        _logger.LogInformation("Remote IP: {RemoteIP}", context.Connection.RemoteIpAddress);
        _logger.LogInformation("Method: {Method}", context.Request.Method);
        _logger.LogInformation("Path: {Path}", context.Request.Path);
        _logger.LogInformation("Query: {Query}", context.Request.QueryString);
        _logger.LogInformation("Protocol: {Protocol}", context.Request.Protocol);
        _logger.LogInformation("Scheme: {Scheme}", context.Request.Scheme);
        _logger.LogInformation("Host: {Host}", context.Request.Host);

        // Log all headers
        _logger.LogInformation("=== REQUEST HEADERS ===");
        foreach (var header in context.Request.Headers)
        {
            _logger.LogInformation("{Name}: {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
        }

        // Log request body for POST requests
        if (context.Request.Method == "POST" && context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();

            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            _logger.LogInformation("=== REQUEST BODY ===");
            _logger.LogInformation("Content-Length: {Length}", context.Request.ContentLength);
            _logger.LogInformation("Body: {Body}", body);
        }

        _logger.LogInformation("=== END REQUEST ===");
    }

    private async Task LogResponseAsync(HttpContext context, MemoryStream responseBody)
    {
        _logger.LogInformation("=== OUTGOING RESPONSE ===");
        _logger.LogInformation("Status Code: {StatusCode}", context.Response.StatusCode);

        // Log response headers
        _logger.LogInformation("=== RESPONSE HEADERS ===");
        foreach (var header in context.Response.Headers)
        {
            _logger.LogInformation("{Name}: {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
        }

        // Log response body
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);

        _logger.LogInformation("=== RESPONSE BODY ===");
        _logger.LogInformation("Content-Length: {Length}", responseBody.Length);
        if (responseBody.Length > 0)
        {
            _logger.LogInformation("Body: {Body}", responseBodyText);
        }

        _logger.LogInformation("=== END RESPONSE ===");
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