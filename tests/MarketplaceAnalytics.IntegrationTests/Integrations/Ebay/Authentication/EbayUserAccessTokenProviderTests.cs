using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MarketplaceAnalytics.IntegrationTests.Integrations.Ebay.Authentication;

public sealed class EbayUserAccessTokenProviderTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RequiredScopes_AreNormalizedAndCachedAsOneScopeSet()
    {
        var authenticationService = new RecordingAuthenticationService(FixedNow);
        using var cache = new EbayUserAccessTokenCache();
        var provider = CreateProvider(authenticationService, cache);

        var first = await provider.GetAccessTokenAsync(["scope.two", "scope.one", "scope.one"]);
        var second = await provider.GetAccessTokenAsync(["scope.one", "scope.two"]);

        Assert.Same(first, second);
        Assert.Equal(1, authenticationService.RefreshCount);
        Assert.Equal(["scope.one", "scope.two"], authenticationService.RequestedScopes);
    }

    [Fact]
    public async Task ConcurrentRequests_TriggerOneRefreshForTheSameScopes()
    {
        var authenticationService = new RecordingAuthenticationService(
            FixedNow,
            TimeSpan.FromMilliseconds(50));
        using var cache = new EbayUserAccessTokenCache();
        var firstProvider = CreateProvider(authenticationService, cache);
        var secondProvider = CreateProvider(authenticationService, cache);

        var requests = Enumerable.Range(0, 12)
            .Select(index => (index % 2 == 0 ? firstProvider : secondProvider)
                .GetAccessTokenAsync(["scope.one"]))
            .ToArray();

        var tokens = await Task.WhenAll(requests);

        Assert.Equal(1, authenticationService.RefreshCount);
        Assert.All(tokens, token => Assert.Same(tokens[0], token));
    }

    [Fact]
    public async Task DifferentRequiredScopeSets_AreCachedSeparately()
    {
        var authenticationService = new RecordingAuthenticationService(FixedNow);
        using var cache = new EbayUserAccessTokenCache();
        var provider = CreateProvider(authenticationService, cache);

        await provider.GetAccessTokenAsync(["scope.inventory"]);
        await provider.GetAccessTokenAsync(["scope.fulfillment"]);

        Assert.Equal(2, authenticationService.RefreshCount);
    }

    [Fact]
    public async Task InvalidScopes_FailBeforeReadingRefreshToken()
    {
        var authenticationService = new RecordingAuthenticationService(FixedNow);
        var refreshTokenSource = new RecordingRefreshTokenSource();
        using var cache = new EbayUserAccessTokenCache();
        var provider = new EbayUserAccessTokenProvider(
            authenticationService,
            refreshTokenSource,
            cache,
            new FixedTimeProvider(FixedNow));

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetAccessTokenAsync([]));

        Assert.Equal(0, refreshTokenSource.ReadCount);
        Assert.Equal(0, authenticationService.RefreshCount);
    }

    [Fact]
    public async Task MissingConfiguredRefreshToken_FailsWithoutLeakingSecretMaterial()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MarketplaceAnalytics:Ebay:OAuth:Enabled"] = "false"
                })
            .Build();
        services.AddEbayAuthentication(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IEbayUserAccessTokenProvider>();

        var exception = await Assert.ThrowsAsync<EbayAuthenticationException>(
            () => provider.GetAccessTokenAsync(["scope.one"]));

        Assert.Equal("refresh_token_not_configured", exception.ErrorCode);
        Assert.DoesNotContain("Bearer", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static EbayUserAccessTokenProvider CreateProvider(
        RecordingAuthenticationService authenticationService,
        EbayUserAccessTokenCache cache)
    {
        return new EbayUserAccessTokenProvider(
            authenticationService,
            new RecordingRefreshTokenSource(),
            cache,
            new FixedTimeProvider(FixedNow));
    }

    private sealed class RecordingRefreshTokenSource : IEbayRefreshTokenSource
    {
        public int ReadCount { get; private set; }

        public ValueTask<string> GetRefreshTokenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            return ValueTask.FromResult("FAKE_REFRESH_TOKEN");
        }
    }

    private sealed class RecordingAuthenticationService(
        DateTimeOffset now,
        TimeSpan? delay = null)
        : IEbayAuthenticationService
    {
        private int _refreshCount;

        public int RefreshCount => _refreshCount;

        public IReadOnlyCollection<string> RequestedScopes { get; private set; } = [];

        public Task<Uri> GetUserAuthorizationUriAsync(
            IReadOnlyCollection<string>? scopes = null,
            string? state = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<EbayOAuthTokenResult> ExchangeAuthorizationCodeAsync(
            string authorizationCode,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async Task<EbayOAuthTokenResult> RefreshUserTokenAsync(
            string refreshToken,
            IReadOnlyCollection<string>? scopes = null,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _refreshCount);
            RequestedScopes = scopes ?? [];
            if (delay is not null)
            {
                await Task.Delay(delay.Value, cancellationToken);
            }

            return new EbayOAuthTokenResult(
                new EbayAccessToken("FAKE_ACCESS_TOKEN", "Bearer", now.AddMinutes(10)),
                null,
                RequestedScopes);
        }

        public Task<EbayOAuthTokenResult> AcquireApplicationTokenAsync(
            IReadOnlyCollection<string>? scopes = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
