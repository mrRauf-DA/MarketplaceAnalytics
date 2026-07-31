namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Finances;

public sealed record EbayTransactionQuery(EbayPageRequest Page, EbayDateRange? TransactionDateRange = null, string? TransactionStatus = null, string? TransactionType = null);
public sealed record EbayPayoutQuery(EbayPageRequest Page, EbayDateRange? PayoutDateRange = null, string? PayoutStatus = null);
