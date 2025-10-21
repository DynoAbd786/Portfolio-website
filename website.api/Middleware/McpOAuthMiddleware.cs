using Microsoft.AspNetCore.Authorization;
using website.api.Services;

namespace website.api.Middleware;

/// <summary>
/// MCP OAuth Authentication Middleware for RFC 6749 + RFC 8707 compliance
/// </summary>
public class McpOAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpOAuthMiddleware> _logger;
    private readonly string _baseUrl;

    public McpOAuthMiddleware(RequestDelegate next, ILogger<McpOAuthMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _baseUrl = configuration["BaseUrl"] ?? "https://mkkai.dev";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply OAuth to MCP endpoints
        if (!context.Request.Path.StartsWithSegments("/api/mcp"))
        {
            await _next(context);
            return;
        }

        // Skip OAuth endpoints themselves
        if (context.Request.Path.StartsWithSegments("/api/oauth"))
        {
            await _next(context);
            return;
        }

        // Skip GET requests for initial discovery (allow unauthorized metadata access)
        if (context.Request.Method == "GET")
        {
            await _next(context);
            return;
        }

        _logger.LogInformation("=== MCP OAUTH MIDDLEWARE ===");
        _logger.LogInformation("Path: {Path}", context.Request.Path);
        _logger.LogInformation("Method: {Method}", context.Request.Method);
        _logger.LogInformation("Query String: {QueryString}", context.Request.QueryString);
        _logger.LogInformation("Content Type: {ContentType}", context.Request.ContentType);
        _logger.LogInformation("Content Length: {ContentLength}", context.Request.ContentLength);

        // Log all headers in OAuth middleware
        _logger.LogInformation("=== ALL REQUEST HEADERS ===");
        foreach (var header in context.Request.Headers)
        {
            _logger.LogInformation("Header {Name}: {Value}", header.Key, string.Join(", ", header.Value.ToArray()));
        }

        // Get OAuth service from the scoped service provider
        var oauthService = context.RequestServices.GetRequiredService<IOAuthService>();

        // Check for Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogInformation("No valid authorization header found");
            await ChallengeAsync(context);
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Validate token with RFC 8707 resource binding
        var expectedResource = $"{_baseUrl}/api/mcp";
        var isValid = await oauthService.ValidateAccessTokenAsync(token, expectedResource);

        if (!isValid)
        {
            _logger.LogWarning("Token validation failed");
            await ChallengeAsync(context);
            return;
        }

        // Token is valid, add token info to context
        var tokenInfo = await oauthService.GetTokenInfoAsync(token);
        if (tokenInfo != null)
        {
            context.Items["oauth_client_id"] = tokenInfo.ClientId;
            context.Items["oauth_scope"] = tokenInfo.Scope;
            context.Items["oauth_resource"] = tokenInfo.Resource;
        }

        _logger.LogInformation("OAuth authentication successful");
        await _next(context);
    }

    private async Task ChallengeAsync(HttpContext context)
    {
        _logger.LogInformation("=== OAUTH CHALLENGE ===");

        // Set WWW-Authenticate header per MCP specification
        var authServerUrl = $"{_baseUrl}/api/oauth/.well-known/oauth-authorization-server";
        var wwwAuthenticate = $"Bearer realm=\"MCP\", authorization_uri=\"{_baseUrl}/api/oauth/authorize\", resource_metadata=\"{authServerUrl}\"";

        context.Response.Headers["WWW-Authenticate"] = wwwAuthenticate;
        context.Response.Headers["Cache-Control"] = "no-cache";

        _logger.LogInformation("WWW-Authenticate header: {Header}", wwwAuthenticate);

        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized: OAuth token required for MCP access");
    }
}

/// <summary>
/// Extension method to register the MCP OAuth middleware
/// </summary>
public static class McpOAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseMcpOAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<McpOAuthMiddleware>();
    }
}