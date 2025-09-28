using System.Text.Json.Serialization;

namespace website.api.Models;

/// <summary>
/// OAuth Authorization Server Metadata (RFC 8414)
/// </summary>
public class AuthorizationServerMetadata
{
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = "";

    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = "";

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = "";

    [JsonPropertyName("response_types_supported")]
    public List<string> ResponseTypesSupported { get; set; } = new() { "code" };

    [JsonPropertyName("grant_types_supported")]
    public List<string> GrantTypesSupported { get; set; } = new() { "authorization_code", "refresh_token" };

    [JsonPropertyName("code_challenge_methods_supported")]
    public List<string> CodeChallengeMethodsSupported { get; set; } = new() { "S256" };

    [JsonPropertyName("scopes_supported")]
    public List<string> ScopesSupported { get; set; } = new() { "mcp:tools", "mcp:resources" };

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public List<string> TokenEndpointAuthMethodsSupported { get; set; } = new() { "none", "client_secret_post" };

    [JsonPropertyName("registration_endpoint")]
    public string? RegistrationEndpoint { get; set; }

    [JsonPropertyName("resource_indicators_supported")]
    public bool ResourceIndicatorsSupported { get; set; } = true;
}

/// <summary>
/// OAuth Client Registration (RFC 7591)
/// </summary>
public class ClientRegistrationRequest
{
    [JsonPropertyName("redirect_uris")]
    public List<string> RedirectUris { get; set; } = new();

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = "none";

    [JsonPropertyName("grant_types")]
    public List<string> GrantTypes { get; set; } = new() { "authorization_code", "refresh_token" };

    [JsonPropertyName("response_types")]
    public List<string> ResponseTypes { get; set; } = new() { "code" };

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

/// <summary>
/// OAuth Client Registration Response (RFC 7591)
/// </summary>
public class ClientRegistrationResponse
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("registration_access_token")]
    public string? RegistrationAccessToken { get; set; }

    [JsonPropertyName("registration_client_uri")]
    public string? RegistrationClientUri { get; set; }

    [JsonPropertyName("client_id_issued_at")]
    public long? ClientIdIssuedAt { get; set; }

    [JsonPropertyName("client_secret_expires_at")]
    public long? ClientSecretExpiresAt { get; set; }

    [JsonPropertyName("redirect_uris")]
    public List<string> RedirectUris { get; set; } = new();

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = "none";

    [JsonPropertyName("grant_types")]
    public List<string> GrantTypes { get; set; } = new() { "authorization_code", "refresh_token" };

    [JsonPropertyName("response_types")]
    public List<string> ResponseTypes { get; set; } = new() { "code" };
}

/// <summary>
/// OAuth Authorization Request
/// </summary>
public class AuthorizationRequest
{
    public string ResponseType { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string? RedirectUri { get; set; }
    public string? Scope { get; set; }
    public string? State { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Resource { get; set; } // RFC 8707
}

/// <summary>
/// OAuth Token Request
/// </summary>
public class TokenRequest
{
    public string GrantType { get; set; } = "";
    public string? Code { get; set; }
    public string? RedirectUri { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? CodeVerifier { get; set; }
    public string? RefreshToken { get; set; }
    public string? Resource { get; set; } // RFC 8707
}

/// <summary>
/// OAuth Token Response
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 3600;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

/// <summary>
/// OAuth Error Response
/// </summary>
public class OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = "";

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("error_uri")]
    public string? ErrorUri { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}

/// <summary>
/// Internal OAuth Client Storage
/// </summary>
public class OAuthClient
{
    public string ClientId { get; set; } = "";
    public string? ClientSecret { get; set; }
    public List<string> RedirectUris { get; set; } = new();
    public string TokenEndpointAuthMethod { get; set; } = "none";
    public List<string> GrantTypes { get; set; } = new() { "authorization_code", "refresh_token" };
    public List<string> ResponseTypes { get; set; } = new() { "code" };
    public string? ClientName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Internal Authorization Code Storage
/// </summary>
public class AuthorizationCode
{
    public string Code { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string? RedirectUri { get; set; }
    public string? Scope { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Resource { get; set; } // RFC 8707
    public string? State { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);
    public bool IsUsed { get; set; } = false;
}

/// <summary>
/// Internal Access Token Storage
/// </summary>
public class AccessToken
{
    public string Token { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string? Scope { get; set; }
    public string? Resource { get; set; } // RFC 8707 - Audience
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
    public string? RefreshToken { get; set; }
}