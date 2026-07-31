namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Fulfillment;

public sealed record EbayOrderQuery(EbayPageRequest Page, EbayDateRange? CreationDateRange = null, EbayDateRange? LastModifiedDateRange = null, string? FulfillmentStatus = null);
