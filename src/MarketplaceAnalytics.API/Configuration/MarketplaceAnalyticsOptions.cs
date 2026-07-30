namespace MarketplaceAnalytics.API.Configuration;

public sealed class MarketplaceAnalyticsOptions
{
    public const string SectionName = "MarketplaceAnalytics";

    public string ApplicationName { get; init; } = string.Empty;

    public string DataDirectory { get; init; } = string.Empty;
}
