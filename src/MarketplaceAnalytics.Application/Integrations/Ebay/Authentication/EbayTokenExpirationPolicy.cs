namespace MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

public static class EbayTokenExpirationPolicy
{
    public static TimeSpan SafetyWindow { get; } = TimeSpan.FromMinutes(1);

    public static bool IsUsable(
        DateTimeOffset expiresAtUtc,
        DateTimeOffset utcNow)
    {
        return utcNow < expiresAtUtc - SafetyWindow;
    }
}
