namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayOAuthOptions
{
    public const string SectionName = "MarketplaceAnalytics:Ebay:OAuth";

    public bool Enabled { get; init; }

    public EbayEnvironment Environment { get; init; } = EbayEnvironment.Sandbox;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string RedirectUriName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> DefaultScopes { get; init; } = [];

    public int RequestTimeoutSeconds { get; init; } = 30;
}
