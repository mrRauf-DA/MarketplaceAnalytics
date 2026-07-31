using System.Text.Json.Serialization;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Transport;

internal sealed class EbayMoneyDto { public decimal? Value { get; init; } public string? Currency { get; init; } }
internal sealed class EbayInventoryPageDto { public List<EbayInventoryItemDto>? InventoryItems { get; init; } public long? Total { get; init; } public int? Limit { get; init; } public int? Offset { get; init; } public string? Next { get; init; } public string? Prev { get; init; } }
internal sealed class EbayInventoryItemDto { public string? Sku { get; init; } public string? Locale { get; init; } public string? Condition { get; init; } public EbayProductDto? Product { get; init; } public EbayAvailabilityDto? Availability { get; init; } public EbayPackageDto? PackageWeightAndSize { get; init; } }
internal sealed class EbayProductDto { public string? Title { get; init; } public string? Description { get; init; } public Dictionary<string, List<string>>? Aspects { get; init; } }
internal sealed class EbayAvailabilityDto { public EbayShipAvailabilityDto? ShipToLocationAvailability { get; init; } }
internal sealed class EbayShipAvailabilityDto { public int? Quantity { get; init; } }
internal sealed class EbayPackageDto { public EbayWeightDto? Weight { get; init; } public EbayDimensionsDto? Dimensions { get; init; } public string? PackageType { get; init; } }
internal sealed class EbayWeightDto { public decimal? Value { get; init; } public string? Unit { get; init; } }
internal sealed class EbayDimensionsDto { public decimal? Height { get; init; } public decimal? Length { get; init; } public decimal? Width { get; init; } public string? Unit { get; init; } }
internal sealed class EbayOrderPageDto { public List<EbayOrderDto>? Orders { get; init; } public long? Total { get; init; } public int? Limit { get; init; } public int? Offset { get; init; } public string? Next { get; init; } public string? Prev { get; init; } }
internal sealed class EbayOrderDto { public string? OrderId { get; init; } public string? OrderFulfillmentStatus { get; init; } public DateTimeOffset? CreationDate { get; init; } public DateTimeOffset? LastModifiedDate { get; init; } public EbayPricingSummaryDto? PricingSummary { get; init; } public List<EbayLineItemDto>? LineItems { get; init; } }
internal sealed class EbayPricingSummaryDto { public EbayMoneyDto? Total { get; init; } }
internal sealed class EbayLineItemDto { public string? LineItemId { get; init; } public string? LegacyItemId { get; init; } public string? Sku { get; init; } public string? Title { get; init; } public int? Quantity { get; init; } public EbayMoneyDto? LineItemCost { get; init; } }
internal sealed class EbayTransactionPageDto { public List<EbayTransactionDto>? Transactions { get; init; } public long? Total { get; init; } public int? Limit { get; init; } public int? Offset { get; init; } public string? Next { get; init; } public string? Prev { get; init; } }
internal sealed class EbayTransactionDto { public string? TransactionId { get; init; } public string? TransactionType { get; init; } public string? TransactionStatus { get; init; } public string? OrderId { get; init; } public string? LineItemId { get; init; } public string? PayoutId { get; init; } public string? BookingEntry { get; init; } public DateTimeOffset? TransactionDate { get; init; } public EbayMoneyDto? Amount { get; init; } public EbayMoneyDto? TotalFeeBasisAmount { get; init; } public EbayMoneyDto? TotalFeeAmount { get; init; } public List<EbayTransactionLineItemDto>? OrderLineItems { get; init; } }
internal sealed class EbayTransactionLineItemDto { public string? LineItemId { get; init; } public EbayMoneyDto? FeeBasisAmount { get; init; } }
internal sealed class EbayPayoutPageDto { public List<EbayPayoutDto>? Payouts { get; init; } public long? Total { get; init; } public int? Limit { get; init; } public int? Offset { get; init; } public string? Next { get; init; } public string? Prev { get; init; } }
internal sealed class EbayPayoutDto { public string? PayoutId { get; init; } public string? PayoutStatus { get; init; } public EbayMoneyDto? Amount { get; init; } public DateTimeOffset? PayoutDate { get; init; } public DateTimeOffset? LastAttemptedPayoutDate { get; init; } public int? TransactionCount { get; init; } }
internal sealed class EbayErrorEnvelopeDto { public List<EbayErrorDto>? Errors { get; init; } }
internal sealed class EbayErrorDto { public long? ErrorId { get; init; } public string? Domain { get; init; } public string? Category { get; init; } public string? Subdomain { get; init; } public string? Message { get; init; } public string? LongMessage { get; init; } }
