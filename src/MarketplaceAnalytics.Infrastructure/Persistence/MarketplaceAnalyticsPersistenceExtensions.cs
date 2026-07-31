using MarketplaceAnalytics.Infrastructure.Persistence.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MarketplaceAnalytics.Infrastructure.Persistence;

public static class MarketplaceAnalyticsPersistenceExtensions
{
    public const string ConnectionStringName = "MarketplaceAnalyticsDatabase";
    public const string HealthCheckName = "postgresql_database";

    public static IServiceCollection AddMarketplaceAnalyticsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is required.");
        }

        services.AddDbContext<MarketplaceAnalyticsDbContext>(
            options => PostgreSqlOptions.Configure(options, connectionString));

        services
            .AddHealthChecks()
            .AddCheck<PostgreSqlDatabaseHealthCheck>(
                HealthCheckName,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "ready"]);

        return services;
    }
}
