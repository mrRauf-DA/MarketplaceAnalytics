using System.Collections.Concurrent;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayUserAccessTokenCache : IDisposable
{
    private readonly ConcurrentDictionary<string, EbayAccessToken> _tokens = new(StringComparer.Ordinal);

    public SemaphoreSlim RefreshGate { get; } = new(1, 1);

    public bool TryGetUsable(string scopeKey, DateTimeOffset utcNow, out EbayAccessToken? accessToken)
    {
        if (_tokens.TryGetValue(scopeKey, out var cachedToken) && cachedToken.IsUsableAt(utcNow))
        {
            accessToken = cachedToken;
            return true;
        }

        accessToken = null;
        return false;
    }

    public void Set(string scopeKey, EbayAccessToken accessToken)
    {
        _tokens[scopeKey] = accessToken;
    }

    public void Dispose()
    {
        RefreshGate.Dispose();
    }
}
