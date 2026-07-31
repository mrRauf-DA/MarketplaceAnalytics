namespace MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

public sealed record EbayOAuthTokenResult(
    EbayAccessToken AccessToken,
    EbayRefreshToken? RefreshToken,
    IReadOnlyCollection<string> GrantedScopes);
