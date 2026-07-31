namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Inventory;

public interface IEbayInventoryClient
{
    Task<EbayApiPage<EbayInventoryItem>> GetInventoryItemsAsync(EbayPageRequest page, CancellationToken cancellationToken = default);
    Task<EbayInventoryItem?> GetInventoryItemAsync(string sku, CancellationToken cancellationToken = default);
}
