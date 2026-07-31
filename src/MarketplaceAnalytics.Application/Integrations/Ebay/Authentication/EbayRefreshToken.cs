namespace MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

public sealed record EbayRefreshToken(
    string Value,
    DateTimeOffset? ExpiresAtUtc);
