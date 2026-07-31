namespace MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

/// <summary>
/// Supplies usable eBay user access tokens for an explicit set of required scopes.
/// </summary>
public interface IEbayUserAccessTokenProvider
{
    /// <summary>
    /// Gets a usable user access token covering the requested scopes.
    /// </summary>
    Task<EbayAccessToken> GetAccessTokenAsync(
        IReadOnlyCollection<string> requiredScopes,
        CancellationToken cancellationToken = default);
}
