namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayUserAccessTokenOptions
{
    public const string SectionName = "MarketplaceAnalytics:Ebay:UserAccessToken";

    public string RefreshToken { get; init; } = string.Empty;
}
