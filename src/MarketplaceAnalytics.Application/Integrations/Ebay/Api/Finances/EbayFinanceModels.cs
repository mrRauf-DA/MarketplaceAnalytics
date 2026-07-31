namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Finances;

public sealed record EbayTransaction(string TransactionId, string? TransactionType, string? TransactionStatus, string? OrderId, string? LineItemId, string? PayoutId, string? BookingEntry, DateTimeOffset? TransactionDate, EbayMoney? Amount, EbayMoney? TotalFeeBasisAmount, EbayMoney? TotalFeeAmount, IReadOnlyList<EbayTransactionLineItem> OrderLineItems);
public sealed record EbayTransactionLineItem(string? LineItemId, EbayMoney? FeeBasisAmount);
public sealed record EbayPayout(string PayoutId, string? PayoutStatus, EbayMoney? Amount, DateTimeOffset? PayoutDate, DateTimeOffset? LastAttemptedPayoutDate, int? TransactionCount);
