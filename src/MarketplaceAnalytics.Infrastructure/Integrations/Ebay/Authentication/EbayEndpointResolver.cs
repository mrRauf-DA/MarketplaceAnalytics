namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayEndpointResolver
{
    private const string TokenPath = "/identity/v1/oauth2/token";
    private const string AuthorizationPath = "/oauth2/authorize";

    public EbayOAuthEndpoints Resolve(EbayEnvironment environment)
    {
        var (authorizationHost, apiHost) = environment switch
        {
            EbayEnvironment.Sandbox => (
                "https://auth.sandbox.ebay.com",
                "https://api.sandbox.ebay.com"),
            EbayEnvironment.Production => (
                "https://auth.ebay.com",
                "https://api.ebay.com"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(environment),
                environment,
                "Unsupported eBay environment.")
        };

        return new EbayOAuthEndpoints(
            new Uri(authorizationHost + AuthorizationPath),
            new Uri(apiHost + TokenPath),
            new Uri(apiHost + "/"));
    }
}
