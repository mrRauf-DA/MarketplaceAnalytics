using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using Xunit;

namespace MarketplaceAnalytics.UnitTests.Application.Integrations.Ebay.Authentication;

public sealed class EbayTokenExpirationPolicyTests
{
    [Fact]
    public void TokenOutsideSafetyWindow_IsUsable()
    {
        var now = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var token = new EbayAccessToken("FAKE_ACCESS_TOKEN", "Bearer", now.AddMinutes(2));

        Assert.True(token.IsUsableAt(now));
    }

    [Fact]
    public void TokenInsideSafetyWindow_IsNotUsable()
    {
        var now = new DateTimeOffset(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);
        var token = new EbayAccessToken("FAKE_ACCESS_TOKEN", "Bearer", now.AddSeconds(59));

        Assert.False(token.IsUsableAt(now));
    }
}
