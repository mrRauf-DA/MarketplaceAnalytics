namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal interface IEbayRefreshTokenSource
{
    ValueTask<string> GetRefreshTokenAsync(CancellationToken cancellationToken);
}
