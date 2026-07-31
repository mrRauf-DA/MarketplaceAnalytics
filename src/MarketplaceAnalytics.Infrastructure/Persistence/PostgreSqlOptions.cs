using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace MarketplaceAnalytics.Infrastructure.Persistence;

internal static class PostgreSqlOptions
{
    public static void Configure(
        DbContextOptionsBuilder optionsBuilder,
        string connectionString)
    {
        optionsBuilder.UseNpgsql(
            connectionString,
            ConfigureProvider);
    }

    private static void ConfigureProvider(
        NpgsqlDbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.MigrationsAssembly(
            typeof(MarketplaceAnalyticsDbContext).Assembly.FullName);
        optionsBuilder.MigrationsHistoryTable(
            MarketplaceAnalyticsDbContext.MigrationsHistoryTableName,
            MarketplaceAnalyticsDbContext.DefaultSchema);
    }
}
