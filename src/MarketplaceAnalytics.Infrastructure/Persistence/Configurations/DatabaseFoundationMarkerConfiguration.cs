using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketplaceAnalytics.Infrastructure.Persistence.Configurations;

internal sealed class DatabaseFoundationMarkerConfiguration
    : IEntityTypeConfiguration<DatabaseFoundationMarker>
{
    public void Configure(EntityTypeBuilder<DatabaseFoundationMarker> builder)
    {
        builder.ToTable("database_foundation_marker");

        builder.HasKey(marker => marker.Id);

        builder.Property(marker => marker.Id)
            .ValueGeneratedNever();

        builder.Property(marker => marker.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");
    }
}
