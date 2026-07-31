using MarketplaceAnalytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace MarketplaceAnalytics.IntegrationTests.Persistence;

public sealed class MarketplaceAnalyticsPersistenceTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=marketplace_analytics_tests;Username=test;Password=test";

    [Fact]
    public void ValidConnectionString_RegistersPersistenceServices()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(TestConnectionString);

        services.AddMarketplaceAnalyticsPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        var healthChecks = provider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;

        Assert.Contains(
            healthChecks.Registrations,
            registration => registration.Name
                == MarketplaceAnalyticsPersistenceExtensions.HealthCheckName);
    }

    [Fact]
    public void MissingConnectionString_FailsClearly()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddMarketplaceAnalyticsPersistence(configuration));

        Assert.Equal(
            "Connection string 'ConnectionStrings:MarketplaceAnalyticsDatabase' is required.",
            exception.Message);
    }

    [Fact]
    public void BlankConnectionString_FailsClearly()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("   ");

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddMarketplaceAnalyticsPersistence(configuration));

        Assert.Equal(
            "Connection string 'ConnectionStrings:MarketplaceAnalyticsDatabase' is required.",
            exception.Message);
    }

    [Fact]
    public void RegisteredDbContext_CanBeResolved()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(TestConnectionString);
        services.AddMarketplaceAnalyticsPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<MarketplaceAnalyticsDbContext>();

        Assert.NotNull(dbContext);
    }

    [Fact]
    public void RegisteredDbContext_UsesPostgreSqlProvider()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(TestConnectionString);
        services.AddMarketplaceAnalyticsPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<MarketplaceAnalyticsDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", dbContext.Database.ProviderName);
    }

    private static IConfiguration BuildConfiguration(string? connectionString)
    {
        var values = new Dictionary<string, string?>();
        if (connectionString is not null)
        {
            values["ConnectionStrings:MarketplaceAnalyticsDatabase"] = connectionString;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
