using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

public static class EbayAuthenticationExtensions
{
    public static IServiceCollection AddEbayAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<EbayOAuthOptions>()
            .Bind(configuration.GetSection(EbayOAuthOptions.SectionName))
            .ValidateOnStart();
        services
            .AddOptions<EbayUserAccessTokenOptions>()
            .Bind(configuration.GetSection(EbayUserAccessTokenOptions.SectionName));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<EbayOAuthOptions>, EbayOAuthOptionsValidator>());

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<EbayEndpointResolver>();
        services.TryAddSingleton<IEbayRefreshTokenSource, ConfigurationEbayRefreshTokenSource>();
        services.TryAddSingleton<EbayUserAccessTokenCache>();
        services.TryAddTransient<IEbayUserAccessTokenProvider, EbayUserAccessTokenProvider>();
        services
            .AddHttpClient<IEbayAuthenticationService, EbayAuthenticationService>(
                (serviceProvider, client) =>
                {
                    var oauthOptions = serviceProvider
                        .GetRequiredService<IOptions<EbayOAuthOptions>>()
                        .Value;
                    client.Timeout = TimeSpan.FromSeconds(oauthOptions.RequestTimeoutSeconds);
                });

        return services;
    }
}
