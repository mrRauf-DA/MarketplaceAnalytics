using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using Microsoft.Extensions.Options;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class ConfigurationEbayRefreshTokenSource(
    IOptions<EbayUserAccessTokenOptions> options)
    : IEbayRefreshTokenSource
{
    public ValueTask<string> GetRefreshTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(options.Value.RefreshToken))
        {
            throw new EbayAuthenticationException(
                "The eBay user refresh-token source is not configured.",
                "refresh_token_not_configured");
        }

        return ValueTask.FromResult(options.Value.RefreshToken);
    }
}
