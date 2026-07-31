using MarketplaceAnalytics.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAnalytics.Infrastructure.Persistence;

public sealed class MarketplaceAnalyticsDbContext(
    DbContextOptions<MarketplaceAnalyticsDbContext> options)
    : DbContext(options)
{
    public const string DefaultSchema = "marketplace_analytics";
    public const string MigrationsHistoryTableName = "__ef_migrations_history";

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketplaceAnalyticsDbContext).Assembly);

        SnakeCaseModelConvention.Apply(modelBuilder.Model);
    }
}
