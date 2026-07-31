using MarketplaceAnalytics.Application.Integrations.Ebay.Api;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Fulfillment;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Transport;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Fulfillment;

internal sealed class EbayFulfillmentClient(HttpClient httpClient, IEbayUserAccessTokenProvider tokenProvider)
    : EbayApiClientBase(httpClient, tokenProvider), IEbayFulfillmentClient
{
    public async Task<EbayApiPage<EbayOrder>> GetOrdersAsync(EbayOrderQuery query, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<EbayOrderPageDto>(EbayApiQueryBuilder.Orders(query), EbayApiScopes.Fulfillment, "GetOrders", cancellationToken);
        return new EbayApiPage<EbayOrder>((response.Orders ?? []).Select(Map).ToArray(), response.Total, response.Limit, response.Offset, ToUri(response.Next), ToUri(response.Prev));
    }

    public async Task<EbayOrder?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var path = EbayApiQueryBuilder.EncodedPath("sell/fulfillment/v1/order/", orderId, nameof(orderId));
        var response = await GetOptionalAsync<EbayOrderDto>(path, EbayApiScopes.Fulfillment, "GetOrder", cancellationToken);
        return response is null ? null : Map(response);
    }

    private static EbayOrder Map(EbayOrderDto value) => new(RequiredIdentifier(value.OrderId, "order ID", "MapOrder"), value.OrderFulfillmentStatus, value.CreationDate, value.LastModifiedDate, ToMoney(value.PricingSummary?.Total), (value.LineItems ?? []).Select(item => new EbayOrderLineItem(RequiredIdentifier(item.LineItemId, "line-item ID", "MapOrder"), item.LegacyItemId, item.Sku, item.Title, item.Quantity, ToMoney(item.LineItemCost))).ToArray());
}
