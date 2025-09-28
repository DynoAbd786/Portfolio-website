using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using website.api.Models;
using website.api.Services;
using System.Web;

namespace website.api.Controllers;

[ApiController]
[Route("api/oauth")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IOAuthService oauthService, ILogger<OAuthController> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    /// <summary>
    /// OAuth Authorization Server Metadata Endpoint (RFC 8414)
    /// </summary>
    [HttpGet(".well-known/oauth-authorization-server")]
    public async Task<IActionResult> GetAuthorizationServerMetadata()
    {
        _logger.LogInformation("=== OAUTH AUTHORIZATION SERVER METADATA REQUEST ===");

        var metadata = await _oauthService.GetAuthorizationServerMetadataAsync();
        return Ok(metadata);
    }

    /// <summary>
    /// OAuth Client Registration Endpoint (RFC 7591)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterClient([FromBody] ClientRegistrationRequest request)
    {
        _logger.LogInformation("=== OAUTH CLIENT REGISTRATION REQUEST ===");
        _logger.LogInformation("Request: {@Request}", request);

        try
        {
            var response = await _oauthService.RegisterClientAsync(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Client registration failed: {Message}", ex.Message);
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = ex.Message
            });
        }
    }

    /// <summary>
    /// OAuth Authorization Endpoint (RFC 6749)
    /// </summary>
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string response_type,
        [FromQuery] string client_id,
        [FromQuery] string? redirect_uri,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        [FromQuery] string? resource) // RFC 8707
    {
        _logger.LogInformation("=== OAUTH AUTHORIZATION REQUEST ===");
        _logger.LogInformation("Response Type: {ResponseType}", response_type);
        _logger.LogInformation("Client ID: {ClientId}", client_id);
        _logger.LogInformation("Redirect URI: {RedirectUri}", redirect_uri);
        _logger.LogInformation("Scope: {Scope}", scope);
        _logger.LogInformation("Resource: {Resource}", resource);

        try
        {
            var authRequest = new AuthorizationRequest
            {
                ResponseType = response_type,
                ClientId = client_id,
                RedirectUri = redirect_uri,
                Scope = scope,
                State = state,
                CodeChallenge = code_challenge,
                CodeChallengeMethod = code_challenge_method,
                Resource = resource
            };

            // In a real implementation, you would show a consent screen here
            // For demo purposes, we'll auto-approve the request
            var code = await _oauthService.GenerateAuthorizationCodeAsync(authRequest);

            // Build redirect URL with authorization code (Claude's official callback)
            var redirectUrl = redirect_uri ?? "https://claude.ai/api/mcp/auth_callback";
            var uriBuilder = new UriBuilder(redirectUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["code"] = code;
            if (!string.IsNullOrEmpty(state))
            {
                query["state"] = state;
            }
            uriBuilder.Query = query.ToString();

            _logger.LogInformation("Redirecting to: {RedirectUrl}", uriBuilder.ToString());
            return Redirect(uriBuilder.ToString());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Authorization request failed: {Message}", ex.Message);

            if (!string.IsNullOrEmpty(redirect_uri))
            {
                var errorUrl = $"{redirect_uri}?error=invalid_request&error_description={Uri.EscapeDataString(ex.Message)}";
                if (!string.IsNullOrEmpty(state))
                {
                    errorUrl += $"&state={Uri.EscapeDataString(state)}";
                }
                return Redirect(errorUrl);
            }

            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = ex.Message,
                State = state
            });
        }
    }

    /// <summary>
    /// OAuth Token Endpoint (RFC 6749)
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request)
    {
        _logger.LogInformation("=== OAUTH TOKEN REQUEST ===");
        _logger.LogInformation("Grant Type: {GrantType}", request.GrantType);
        _logger.LogInformation("Client ID: {ClientId}", request.ClientId);
        _logger.LogInformation("Resource: {Resource}", request.Resource);

        try
        {
            var response = await _oauthService.ExchangeCodeForTokenAsync(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Token exchange failed: {Message}", ex.Message);
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_grant",
                ErrorDescription = ex.Message
            });
        }
    }

    /// <summary>
    /// OAuth Token Introspection Endpoint (RFC 7662) - Optional
    /// </summary>
    [HttpPost("introspect")]
    public async Task<IActionResult> Introspect([FromForm] string token)
    {
        _logger.LogInformation("=== OAUTH TOKEN INTROSPECTION ===");

        var tokenInfo = await _oauthService.GetTokenInfoAsync(token);
        if (tokenInfo == null || tokenInfo.ExpiresAt < DateTime.UtcNow)
        {
            return Ok(new { active = false });
        }

        return Ok(new
        {
            active = true,
            client_id = tokenInfo.ClientId,
            scope = tokenInfo.Scope,
            aud = tokenInfo.Resource,
            exp = ((DateTimeOffset)tokenInfo.ExpiresAt).ToUnixTimeSeconds()
        });
    }
}