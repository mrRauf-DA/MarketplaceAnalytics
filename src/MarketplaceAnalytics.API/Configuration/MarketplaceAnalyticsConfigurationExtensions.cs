using Microsoft.Extensions.Options;

namespace MarketplaceAnalytics.API.Configuration;

public static class MarketplaceAnalyticsConfigurationExtensions
{
    public static IServiceCollection AddMarketplaceAnalyticsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MarketplaceAnalyticsOptions>()
            .Bind(configuration.GetSection(MarketplaceAnalyticsOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApplicationName),
                $"{MarketplaceAnalyticsOptions.SectionName}:ApplicationName is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DataDirectory),
                $"{MarketplaceAnalyticsOptions.SectionName}:DataDirectory is required.")
            .ValidateOnStart();

        return services;
    }
}
