using MarketplaceAnalytics.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace MarketplaceAnalytics.IntegrationTests.Configuration;

public sealed class MarketplaceAnalyticsConfigurationTests
{
    [Fact]
    public void ValidConfiguration_BindsSuccessfully()
    {
        using var provider = BuildServiceProvider("MarketplaceAnalytics", "data");

        var options = provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value;

        Assert.NotNull(options);
    }

    [Fact]
    public void ApplicationName_IsBoundCorrectly()
    {
        using var provider = BuildServiceProvider("Analytics Host", "data");

        var options = provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value;

        Assert.Equal("Analytics Host", options.ApplicationName);
    }

    [Fact]
    public void DataDirectory_IsBoundCorrectly()
    {
        using var provider = BuildServiceProvider("MarketplaceAnalytics", "local-data");

        var options = provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value;

        Assert.Equal("local-data", options.DataDirectory);
    }

    [Fact]
    public void MissingApplicationName_FailsValidation()
    {
        using var provider = BuildServiceProvider(null, "data");

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value);

        Assert.Contains("MarketplaceAnalytics:ApplicationName is required.", exception.Failures);
    }

    [Fact]
    public void BlankApplicationName_FailsValidation()
    {
        using var provider = BuildServiceProvider("   ", "data");

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value);

        Assert.Contains("MarketplaceAnalytics:ApplicationName is required.", exception.Failures);
    }

    [Fact]
    public void MissingDataDirectory_FailsValidation()
    {
        using var provider = BuildServiceProvider("MarketplaceAnalytics", null);

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value);

        Assert.Contains("MarketplaceAnalytics:DataDirectory is required.", exception.Failures);
    }

    [Fact]
    public void BlankDataDirectory_FailsValidation()
    {
        using var provider = BuildServiceProvider("MarketplaceAnalytics", "\t");

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value);

        Assert.Contains("MarketplaceAnalytics:DataDirectory is required.", exception.Failures);
    }

    [Fact]
    public void EnvironmentVariableHierarchy_WhenNormalized_BindsSuccessfully()
    {
        const string applicationNameVariable = "MarketplaceAnalytics__ApplicationName";
        const string dataDirectoryVariable = "MarketplaceAnalytics__DataDirectory";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [NormalizeEnvironmentVariable(applicationNameVariable)] = "Environment Host",
                    [NormalizeEnvironmentVariable(dataDirectoryVariable)] = "environment-data"
                })
            .Build();

        using var provider = BuildServiceProvider(configuration);
        var options = provider.GetRequiredService<IOptions<MarketplaceAnalyticsOptions>>().Value;

        Assert.Equal("Environment Host", options.ApplicationName);
        Assert.Equal("environment-data", options.DataDirectory);
    }

    [Fact]
    public async Task RegisteredOptions_AreValidatedWhenHostStarts()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(
            CreateConfigurationValues(null, "data"));
        builder.Services.AddMarketplaceAnalyticsConfiguration(builder.Configuration);

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync());

        Assert.Contains("MarketplaceAnalytics:ApplicationName is required.", exception.Failures);
    }

    private static ServiceProvider BuildServiceProvider(
        string? applicationName,
        string? dataDirectory)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                CreateConfigurationValues(applicationName, dataDirectory))
            .Build();

        return BuildServiceProvider(configuration);
    }

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddMarketplaceAnalyticsConfiguration(configuration);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> CreateConfigurationValues(
        string? applicationName,
        string? dataDirectory)
    {
        var values = new Dictionary<string, string?>();

        if (applicationName is not null)
        {
            values[$"{MarketplaceAnalyticsOptions.SectionName}:ApplicationName"] = applicationName;
        }

        if (dataDirectory is not null)
        {
            values[$"{MarketplaceAnalyticsOptions.SectionName}:DataDirectory"] = dataDirectory;
        }

        return values;
    }

    private static string NormalizeEnvironmentVariable(string variableName)
    {
        return variableName.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
    }
}
