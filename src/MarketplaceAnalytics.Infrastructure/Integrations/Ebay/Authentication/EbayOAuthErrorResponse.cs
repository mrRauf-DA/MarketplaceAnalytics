using System.Text.Json.Serialization;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayOAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
