namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api;

internal static class EbayApiScopes
{
    public static readonly IReadOnlyCollection<string> Inventory =
        ["https://api.ebay.com/oauth/api_scope/sell.inventory.readonly"];
    public static readonly IReadOnlyCollection<string> Fulfillment =
        ["https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly"];
    public static readonly IReadOnlyCollection<string> Finances =
        ["https://api.ebay.com/oauth/api_scope/sell.finances"];
}
