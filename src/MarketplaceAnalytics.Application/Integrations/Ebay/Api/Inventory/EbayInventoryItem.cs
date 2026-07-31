namespace MarketplaceAnalytics.Application.Integrations.Ebay.Api.Inventory;

public sealed record EbayInventoryItem(string Sku, string? Locale, string? Condition, string? Title, string? Description, IReadOnlyDictionary<string, IReadOnlyList<string>> Aspects, int? AvailableQuantity, EbayPackageWeightAndSize? Package);

public sealed record EbayPackageWeightAndSize(decimal? Weight, string? WeightUnit, decimal? Height, decimal? Length, decimal? Width, string? DimensionUnit, string? PackageType);
