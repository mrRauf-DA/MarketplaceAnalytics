namespace MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

public sealed record EbayAccessToken(
    string Value,
    string TokenType,
    DateTimeOffset ExpiresAtUtc)
{
    public bool IsUsableAt(DateTimeOffset utcNow)
    {
        return EbayTokenExpirationPolicy.IsUsable(ExpiresAtUtc, utcNow);
    }
}
