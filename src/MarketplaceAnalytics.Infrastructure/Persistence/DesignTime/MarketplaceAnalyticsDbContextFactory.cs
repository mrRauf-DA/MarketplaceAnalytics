using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MarketplaceAnalytics.Infrastructure.Persistence.DesignTime;

public sealed class MarketplaceAnalyticsDbContextFactory
    : IDesignTimeDbContextFactory<MarketplaceAnalyticsDbContext>
{
    public MarketplaceAnalyticsDbContext CreateDbContext(string[] args)
    {
        var apiProjectDirectory = FindApiProjectDirectory();
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(
            MarketplaceAnalyticsPersistenceExtensions.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{MarketplaceAnalyticsPersistenceExtensions.ConnectionStringName}' "
                + "is required for design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<MarketplaceAnalyticsDbContext>();
        PostgreSqlOptions.Configure(optionsBuilder, connectionString);

        return new MarketplaceAnalyticsDbContext(optionsBuilder.Options);
    }

    private static string FindApiProjectDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(currentDirectory, "src", "MarketplaceAnalytics.API"),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "MarketplaceAnalytics.API")),
            currentDirectory
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate src/MarketplaceAnalytics.API/appsettings.json. "
            + "Run EF Core commands from the repository root or Infrastructure project directory.");
    }
}
