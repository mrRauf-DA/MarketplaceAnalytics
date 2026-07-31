using System.Text.Json.Serialization;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayOAuthResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("refresh_token_expires_in")]
    public long? RefreshTokenExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}
