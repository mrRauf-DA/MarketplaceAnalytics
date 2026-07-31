namespace MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;

public interface IEbayAuthenticationService
{
    Task<Uri> GetUserAuthorizationUriAsync(
        IReadOnlyCollection<string>? scopes = null,
        string? state = null,
        CancellationToken cancellationToken = default);

    Task<EbayOAuthTokenResult> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default);

    Task<EbayOAuthTokenResult> RefreshUserTokenAsync(
        string refreshToken,
        IReadOnlyCollection<string>? scopes = null,
        CancellationToken cancellationToken = default);

    Task<EbayOAuthTokenResult> AcquireApplicationTokenAsync(
        IReadOnlyCollection<string>? scopes = null,
        CancellationToken cancellationToken = default);
}
