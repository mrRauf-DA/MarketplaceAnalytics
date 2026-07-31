using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayUserAccessTokenProvider(
    IEbayAuthenticationService authenticationService,
    IEbayRefreshTokenSource refreshTokenSource,
    EbayUserAccessTokenCache cache,
    TimeProvider timeProvider)
    : IEbayUserAccessTokenProvider
{
    public async Task<EbayAccessToken> GetAccessTokenAsync(
        IReadOnlyCollection<string> requiredScopes,
        CancellationToken cancellationToken = default)
    {
        var normalizedScopes = NormalizeScopes(requiredScopes);
        var scopeKey = string.Join('\n', normalizedScopes);

        if (cache.TryGetUsable(scopeKey, timeProvider.GetUtcNow(), out var cachedToken))
        {
            return cachedToken!;
        }

        await cache.RefreshGate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetUsable(scopeKey, timeProvider.GetUtcNow(), out cachedToken))
            {
                return cachedToken!;
            }

            var refreshToken = await refreshTokenSource.GetRefreshTokenAsync(cancellationToken);
            var result = await authenticationService.RefreshUserTokenAsync(
                refreshToken,
                normalizedScopes,
                cancellationToken);
            cache.Set(scopeKey, result.AccessToken);

            return result.AccessToken;
        }
        finally
        {
            cache.RefreshGate.Release();
        }
    }

    private static IReadOnlyCollection<string> NormalizeScopes(
        IReadOnlyCollection<string> requiredScopes)
    {
        ArgumentNullException.ThrowIfNull(requiredScopes);

        if (requiredScopes.Count == 0 || requiredScopes.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one non-empty eBay OAuth scope is required.",
                nameof(requiredScopes));
        }

        return requiredScopes
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
