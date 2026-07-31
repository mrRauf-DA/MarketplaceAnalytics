namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Fulfillment;

public interface IEbayFulfillmentClient
{
    Task<EbayApiPage<EbayOrder>> GetOrdersAsync(EbayOrderQuery query, CancellationToken cancellationToken = default);
    Task<EbayOrder?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
