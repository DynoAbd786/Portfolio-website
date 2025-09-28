using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using website.api.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace website.api.Services;

public interface IOAuthService
{
    Task<AuthorizationServerMetadata> GetAuthorizationServerMetadataAsync();
    Task<ClientRegistrationResponse> RegisterClientAsync(ClientRegistrationRequest request);
    Task<string> GenerateAuthorizationCodeAsync(AuthorizationRequest request);
    Task<TokenResponse> ExchangeCodeForTokenAsync(TokenRequest request);
    Task<bool> ValidateAccessTokenAsync(string token, string? expectedResource = null);
    Task<AccessToken?> GetTokenInfoAsync(string token);
}

public class OAuthService : IOAuthService
{
    private readonly ILogger<OAuthService> _logger;
    private readonly IConfiguration _configuration;

    // In-memory storage for demo - use database in production
    private static readonly Dictionary<string, OAuthClient> _clients = new();
    private static readonly Dictionary<string, AuthorizationCode> _authCodes = new();
    private static readonly Dictionary<string, AccessToken> _accessTokens = new();

    private readonly string _baseUrl;
    private readonly string _issuer;
    private readonly SymmetricSecurityKey _signingKey;

    public OAuthService(ILogger<OAuthService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _baseUrl = _configuration["BaseUrl"] ?? "https://mkkai.dev";
        _issuer = $"{_baseUrl}/api/oauth";

        // Generate a random signing key for demo - use proper key management in production
        var keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        _signingKey = new SymmetricSecurityKey(keyBytes);
    }

    public Task<AuthorizationServerMetadata> GetAuthorizationServerMetadataAsync()
    {
        var metadata = new AuthorizationServerMetadata
        {
            Issuer = _issuer,
            AuthorizationEndpoint = $"{_baseUrl}/api/oauth/authorize",
            TokenEndpoint = $"{_baseUrl}/api/oauth/token",
            RegistrationEndpoint = $"{_baseUrl}/api/oauth/register",
            ResponseTypesSupported = new() { "code" },
            GrantTypesSupported = new() { "authorization_code", "refresh_token" },
            CodeChallengeMethodsSupported = new() { "S256" },
            ScopesSupported = new() { "mcp:tools", "mcp:resources" },
            TokenEndpointAuthMethodsSupported = new() { "none", "client_secret_post" },
            ResourceIndicatorsSupported = true
        };

        return Task.FromResult(metadata);
    }

    public Task<ClientRegistrationResponse> RegisterClientAsync(ClientRegistrationRequest request)
    {
        _logger.LogInformation("=== OAUTH CLIENT REGISTRATION ===");
        _logger.LogInformation("Client Name: {ClientName}", request.ClientName);
        _logger.LogInformation("Redirect URIs: {RedirectUris}", string.Join(", ", request.RedirectUris));

        var clientId = $"mcp_client_{Guid.NewGuid():N}";
        var client = new OAuthClient
        {
            ClientId = clientId,
            RedirectUris = request.RedirectUris,
            TokenEndpointAuthMethod = request.TokenEndpointAuthMethod,
            GrantTypes = request.GrantTypes,
            ResponseTypes = request.ResponseTypes,
            ClientName = request.ClientName
        };

        _clients[clientId] = client;

        var response = new ClientRegistrationResponse
        {
            ClientId = clientId,
            ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            RedirectUris = request.RedirectUris,
            TokenEndpointAuthMethod = request.TokenEndpointAuthMethod,
            GrantTypes = request.GrantTypes,
            ResponseTypes = request.ResponseTypes
        };

        _logger.LogInformation("Registered client: {ClientId}", clientId);
        return Task.FromResult(response);
    }

    public Task<string> GenerateAuthorizationCodeAsync(AuthorizationRequest request)
    {
        _logger.LogInformation("=== OAUTH AUTHORIZATION CODE GENERATION ===");
        _logger.LogInformation("Client ID: {ClientId}", request.ClientId);
        _logger.LogInformation("Resource: {Resource}", request.Resource);
        _logger.LogInformation("Scope: {Scope}", request.Scope);

        if (!_clients.ContainsKey(request.ClientId))
        {
            throw new ArgumentException("Invalid client_id");
        }

        var client = _clients[request.ClientId];
        if (request.RedirectUri != null && !client.RedirectUris.Contains(request.RedirectUri))
        {
            throw new ArgumentException("Invalid redirect_uri");
        }

        var code = $"auth_code_{Guid.NewGuid():N}";
        var authCode = new AuthorizationCode
        {
            Code = code,
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Resource = request.Resource, // RFC 8707
            State = request.State,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _authCodes[code] = authCode;

        _logger.LogInformation("Generated authorization code: {Code}", code);
        return Task.FromResult(code);
    }

    public Task<TokenResponse> ExchangeCodeForTokenAsync(TokenRequest request)
    {
        _logger.LogInformation("=== OAUTH TOKEN EXCHANGE ===");
        _logger.LogInformation("Grant Type: {GrantType}", request.GrantType);
        _logger.LogInformation("Client ID: {ClientId}", request.ClientId);
        _logger.LogInformation("Resource: {Resource}", request.Resource);

        if (request.GrantType != "authorization_code")
        {
            throw new ArgumentException("Unsupported grant_type");
        }

        if (string.IsNullOrEmpty(request.Code) || !_authCodes.ContainsKey(request.Code))
        {
            throw new ArgumentException("Invalid authorization code");
        }

        var authCode = _authCodes[request.Code];

        if (authCode.IsUsed)
        {
            throw new ArgumentException("Authorization code already used");
        }

        if (authCode.ExpiresAt < DateTime.UtcNow)
        {
            throw new ArgumentException("Authorization code expired");
        }

        if (authCode.ClientId != request.ClientId)
        {
            throw new ArgumentException("Client ID mismatch");
        }

        if (authCode.RedirectUri != request.RedirectUri)
        {
            throw new ArgumentException("Redirect URI mismatch");
        }

        // Validate PKCE if code_challenge was provided
        if (!string.IsNullOrEmpty(authCode.CodeChallenge))
        {
            if (string.IsNullOrEmpty(request.CodeVerifier))
            {
                throw new ArgumentException("Missing code_verifier for PKCE");
            }

            var expectedChallenge = GenerateCodeChallenge(request.CodeVerifier);
            if (authCode.CodeChallenge != expectedChallenge)
            {
                throw new ArgumentException("Invalid code_verifier for PKCE");
            }
        }

        // RFC 8707: Resource parameter validation
        var resource = request.Resource ?? authCode.Resource;
        if (string.IsNullOrEmpty(resource))
        {
            throw new ArgumentException("Resource parameter required (RFC 8707)");
        }

        // Mark code as used
        authCode.IsUsed = true;

        // Generate tokens
        var accessToken = GenerateAccessToken(authCode.ClientId, authCode.Scope, resource);
        var refreshToken = GenerateRefreshToken();

        var tokenInfo = new AccessToken
        {
            Token = accessToken,
            ClientId = authCode.ClientId,
            Scope = authCode.Scope,
            Resource = resource, // RFC 8707 - Audience binding
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RefreshToken = refreshToken
        };

        _accessTokens[accessToken] = tokenInfo;

        var response = new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            RefreshToken = refreshToken,
            Scope = authCode.Scope
        };

        _logger.LogInformation("Generated access token for client: {ClientId}, Resource: {Resource}",
            authCode.ClientId, resource);

        return Task.FromResult(response);
    }

    public Task<bool> ValidateAccessTokenAsync(string token, string? expectedResource = null)
    {
        _logger.LogInformation("=== OAUTH TOKEN VALIDATION ===");
        _logger.LogInformation("Token: {Token}", token.Substring(0, Math.Min(20, token.Length)) + "...");
        _logger.LogInformation("Expected Resource: {ExpectedResource}", expectedResource);

        if (!_accessTokens.ContainsKey(token))
        {
            _logger.LogWarning("Token not found in storage");
            return Task.FromResult(false);
        }

        var tokenInfo = _accessTokens[token];

        if (tokenInfo.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Token has expired");
            return Task.FromResult(false);
        }

        // RFC 8707: Validate audience/resource binding
        if (!string.IsNullOrEmpty(expectedResource) && tokenInfo.Resource != expectedResource)
        {
            _logger.LogWarning("Token audience mismatch. Expected: {Expected}, Actual: {Actual}",
                expectedResource, tokenInfo.Resource);
            return Task.FromResult(false);
        }

        _logger.LogInformation("Token validation successful for client: {ClientId}", tokenInfo.ClientId);
        return Task.FromResult(true);
    }

    public Task<AccessToken?> GetTokenInfoAsync(string token)
    {
        return Task.FromResult(_accessTokens.GetValueOrDefault(token));
    }

    private string GenerateAccessToken(string clientId, string? scope, string? resource)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clientId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("client_id", clientId)
        };

        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        if (!string.IsNullOrEmpty(resource))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, resource)); // RFC 8707
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _issuer,
            Audience = resource,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}