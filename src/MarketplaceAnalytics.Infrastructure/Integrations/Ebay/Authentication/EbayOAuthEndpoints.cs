namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed record EbayOAuthEndpoints(
    Uri AuthorizationEndpoint,
    Uri TokenEndpoint,
    Uri ApiBaseAddress);
