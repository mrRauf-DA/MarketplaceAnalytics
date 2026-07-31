using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Finances;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Fulfillment;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Inventory;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Finances;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Fulfillment;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Inventory;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api;

public static class EbayApiExtensions
{
    public static IServiceCollection AddEbayApiClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        AddClient<IEbayInventoryClient, EbayInventoryClient>(services);
        AddClient<IEbayFulfillmentClient, EbayFulfillmentClient>(services);
        AddClient<IEbayFinancesClient, EbayFinancesClient>(services);
        return services;
    }

    private static void AddClient<TClient, TImplementation>(IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EbayOAuthOptions>>().Value;
            var resolver = serviceProvider.GetRequiredService<EbayEndpointResolver>();
            client.BaseAddress = resolver.Resolve(options.Environment).ApiBaseAddress;
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
    }
}
