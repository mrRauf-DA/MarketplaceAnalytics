namespace MarketplaceAnalytics.Infrastructure.Persistence;

/// <summary>
/// Infrastructure-owned marker used only to establish and verify the database schema.
/// This is not a business-domain entity.
/// </summary>
internal sealed class DatabaseFoundationMarker
{
    public short Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
