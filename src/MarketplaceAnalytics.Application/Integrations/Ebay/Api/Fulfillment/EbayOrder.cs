namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Fulfillment;

public sealed record EbayOrder(string OrderId, string? OrderFulfillmentStatus, DateTimeOffset? CreationDate, DateTimeOffset? LastModifiedDate, EbayMoney? Total, IReadOnlyList<EbayOrderLineItem> LineItems);

public sealed record EbayOrderLineItem(string LineItemId, string? LegacyItemId, string? Sku, string? Title, int? Quantity, EbayMoney? LineItemCost);
