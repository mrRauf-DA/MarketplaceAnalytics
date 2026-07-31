using MarketplaceAnalytics.Application.Integrations.Ebay.Api;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Inventory;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Transport;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Inventory;

internal sealed class EbayInventoryClient(HttpClient httpClient, IEbayUserAccessTokenProvider tokenProvider)
    : EbayApiClientBase(httpClient, tokenProvider), IEbayInventoryClient
{
    public async Task<EbayApiPage<EbayInventoryItem>> GetInventoryItemsAsync(EbayPageRequest page, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        var response = await GetAsync<EbayInventoryPageDto>(EbayApiQueryBuilder.Pagination("sell/inventory/v1/inventory_item", page), EbayApiScopes.Inventory, "GetInventoryItems", cancellationToken);
        return new EbayApiPage<EbayInventoryItem>((response.InventoryItems ?? []).Select(Map).ToArray(), response.Total, response.Limit, response.Offset, ToUri(response.Next), ToUri(response.Prev));
    }

    public async Task<EbayInventoryItem?> GetInventoryItemAsync(string sku, CancellationToken cancellationToken = default)
    {
        var path = EbayApiQueryBuilder.EncodedPath("sell/inventory/v1/inventory_item/", sku, nameof(sku));
        var response = await GetOptionalAsync<EbayInventoryItemDto>(path, EbayApiScopes.Inventory, "GetInventoryItem", cancellationToken);
        return response is null ? null : Map(response);
    }

    private static EbayInventoryItem Map(EbayInventoryItemDto value)
    {
        var package = value.PackageWeightAndSize is null ? null : new EbayPackageWeightAndSize(value.PackageWeightAndSize.Weight?.Value, value.PackageWeightAndSize.Weight?.Unit, value.PackageWeightAndSize.Dimensions?.Height, value.PackageWeightAndSize.Dimensions?.Length, value.PackageWeightAndSize.Dimensions?.Width, value.PackageWeightAndSize.Dimensions?.Unit, value.PackageWeightAndSize.PackageType);
        var aspects = value.Product?.Aspects?.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value, StringComparer.Ordinal) ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        return new EbayInventoryItem(RequiredIdentifier(value.Sku, "SKU", "MapInventoryItem"), value.Locale, value.Condition, value.Product?.Title, value.Product?.Description, aspects, value.Availability?.ShipToLocationAvailability?.Quantity, package);
    }
}
