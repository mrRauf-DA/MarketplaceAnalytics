using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MarketplaceAnalytics.IntegrationTests.Integrations.Ebay.Authentication;

public sealed class EbayAuthenticationTests
{
    private const string FakeClientId = "FAKE_CLIENT_ID";
    private const string FakeClientSecret = "FAKE_CLIENT_SECRET";
    private const string FakeRuName = "FAKE-RUNAME-123";
    private static readonly DateTimeOffset FixedNow =
        new(2026, 7, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SandboxEndpoints_AreResolvedCorrectly()
    {
        var endpoints = new EbayEndpointResolver().Resolve(EbayEnvironment.Sandbox);

        Assert.Equal("https://auth.sandbox.ebay.com/oauth2/authorize", endpoints.AuthorizationEndpoint.AbsoluteUri);
        Assert.Equal("https://api.sandbox.ebay.com/identity/v1/oauth2/token", endpoints.TokenEndpoint.AbsoluteUri);
    }

    [Fact]
    public void ProductionEndpoints_AreResolvedCorrectly()
    {
        var endpoints = new EbayEndpointResolver().Resolve(EbayEnvironment.Production);

        Assert.Equal("https://auth.ebay.com/oauth2/authorize", endpoints.AuthorizationEndpoint.AbsoluteUri);
        Assert.Equal("https://api.ebay.com/identity/v1/oauth2/token", endpoints.TokenEndpoint.AbsoluteUri);
    }

    [Fact]
    public async Task AuthorizationUri_EncodesValuesAndIncludesOptionalState()
    {
        var service = CreateService(options: ValidOptions());

        var uri = await service.GetUserAuthorizationUriAsync(
            ["scope:one", "scope/two"],
            "state value+1");

        Assert.StartsWith("https://auth.sandbox.ebay.com/oauth2/authorize?", uri.AbsoluteUri);
        Assert.Contains("client_id=FAKE_CLIENT_ID", uri.Query);
        Assert.Contains("redirect_uri=FAKE-RUNAME-123", uri.Query);
        Assert.Contains("response_type=code", uri.Query);
        Assert.Contains("scope=scope%3Aone%20scope%2Ftwo", uri.Query);
        Assert.Contains("state=state%20value%2B1", uri.Query);
    }

    [Fact]
    public async Task AuthorizationUri_OmitsStateWhenNotSupplied()
    {
        var service = CreateService(options: ValidOptions());

        var uri = await service.GetUserAuthorizationUriAsync();

        Assert.DoesNotContain("state=", uri.Query);
    }

    [Fact]
    public async Task AuthorizationUri_RejectsEmptyScopes()
    {
        var service = CreateService(options: ValidOptions());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetUserAuthorizationUriAsync([]));
    }

    [Fact]
    public void DisabledOptions_AreValidWithoutCredentials()
    {
        var result = new EbayOAuthOptionsValidator().Validate(
            null,
            new EbayOAuthOptions { Enabled = false });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void EnabledOptions_RequireCredentialsScopesAndValidTimeout()
    {
        var result = new EbayOAuthOptionsValidator().Validate(
            null,
            new EbayOAuthOptions
            {
                Enabled = true,
                DefaultScopes = [""],
                RequestTimeoutSeconds = 0
            });

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures!, failure => failure.Contains("ClientId", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("ClientSecret", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("RedirectUriName", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("DefaultScopes", StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains("RequestTimeoutSeconds", StringComparison.Ordinal));
    }

    [Fact]
    public void UnsupportedEnvironment_FailsValidation()
    {
        var options = WithEnvironment(ValidOptions(), (EbayEnvironment)99);

        var result = new EbayOAuthOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains("Environment must be Sandbox or Production.", result.Failures!);
    }

    [Fact]
    public async Task AuthorizationCodeGrant_SendsExpectedRequest()
    {
        var handler = SuccessHandler();
        var service = CreateService(handler);

        await service.ExchangeAuthorizationCodeAsync("FAKE_AUTH_CODE");

        AssertTokenRequest(handler, "authorization_code");
        Assert.Contains("code=FAKE_AUTH_CODE", handler.Body);
        Assert.Contains("redirect_uri=FAKE-RUNAME-123", handler.Body);
    }

    [Fact]
    public async Task RefreshTokenGrant_SendsTokenAndOptionalScope()
    {
        var handler = SuccessHandler();
        var service = CreateService(handler);

        await service.RefreshUserTokenAsync("FAKE_REFRESH_TOKEN", ["scope one"]);

        AssertTokenRequest(handler, "refresh_token");
        Assert.Contains("refresh_token=FAKE_REFRESH_TOKEN", handler.Body);
        Assert.Contains("scope=scope+one", handler.Body);
    }

    [Fact]
    public async Task ClientCredentialsGrant_SendsConfiguredScopes()
    {
        var handler = SuccessHandler();
        var service = CreateService(handler);

        await service.AcquireApplicationTokenAsync();

        AssertTokenRequest(handler, "client_credentials");
        Assert.Contains("scope=https%3A%2F%2Fapi.ebay.com%2Foauth%2Fapi_scope", handler.Body);
    }

    [Fact]
    public async Task SuccessfulResponse_MapsTokensScopesAndAbsoluteExpiry()
    {
        var handler = SuccessHandler();
        var service = CreateService(handler);

        var result = await service.ExchangeAuthorizationCodeAsync("FAKE_AUTH_CODE");

        Assert.Equal("FAKE_ACCESS_TOKEN", result.AccessToken.Value);
        Assert.Equal("Bearer", result.AccessToken.TokenType);
        Assert.Equal(FixedNow.AddSeconds(7200), result.AccessToken.ExpiresAtUtc);
        Assert.Equal("FAKE_REFRESH_TOKEN", result.RefreshToken?.Value);
        Assert.Equal(FixedNow.AddSeconds(86400), result.RefreshToken?.ExpiresAtUtc);
        Assert.Equal(["scope.one", "scope.two"], result.GrantedScopes);
    }

    [Fact]
    public async Task OAuthError_MapsSafeFieldsWithoutSecrets()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(
                HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_grant\",\"error_description\":\"FAKE_CLIENT_SECRET FAKE_REFRESH_TOKEN\"}")));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<EbayAuthenticationException>(
            () => service.ExchangeAuthorizationCodeAsync("FAKE_AUTH_CODE"));

        Assert.Equal("invalid_grant", exception.ErrorCode);
        Assert.DoesNotContain(FakeClientSecret, exception.Message);
        Assert.DoesNotContain("FAKE_REFRESH_TOKEN", exception.Message);
        Assert.DoesNotContain("FAKE_AUTH_CODE", exception.Message);
    }

    [Fact]
    public async Task MalformedJson_FailsClearly()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, "not-json")));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<EbayAuthenticationException>(
            () => service.AcquireApplicationTokenAsync());

        Assert.Equal("invalid_oauth_response", exception.ErrorCode);
    }

    [Fact]
    public async Task MissingAccessToken_FailsClearly()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                "{\"token_type\":\"Bearer\",\"expires_in\":3600}")));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<EbayAuthenticationException>(
            () => service.AcquireApplicationTokenAsync());

        Assert.Equal("missing_access_token", exception.ErrorCode);
    }

    [Fact]
    public async Task HttpTimeout_IsDistinctFromCallerCancellation()
    {
        var handler = new RecordingHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return JsonResponse(HttpStatusCode.OK, "{}");
            });
        var service = CreateService(handler, timeout: TimeSpan.FromMilliseconds(20));

        var exception = await Assert.ThrowsAsync<EbayAuthenticationException>(
            () => service.AcquireApplicationTokenAsync());

        Assert.Equal("request_timeout", exception.ErrorCode);
    }

    [Fact]
    public async Task CallerCancellation_RemainsCancellation()
    {
        var handler = new RecordingHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return JsonResponse(HttpStatusCode.OK, "{}");
            });
        var service = CreateService(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.AcquireApplicationTokenAsync(cancellationToken: cancellation.Token));
    }

    [Fact]
    public void OAuthTransportDtos_AreInternal()
    {
        Assert.False(typeof(EbayOAuthResponse).IsPublic);
        Assert.False(typeof(EbayOAuthErrorResponse).IsPublic);
    }

    [Fact]
    public async Task DisabledAuthentication_AllowsHostStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["MarketplaceAnalytics:Ebay:OAuth:Enabled"] = "false"
            });
        builder.Services.AddEbayAuthentication(builder.Configuration);

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public async Task EnabledAuthenticationWithMissingSettings_FailsHostStartup()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["MarketplaceAnalytics:Ebay:OAuth:Enabled"] = "true"
            });
        builder.Services.AddEbayAuthentication(builder.Configuration);

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public void Registration_DoesNotReplaceCustomTimeProvider()
    {
        var services = new ServiceCollection();
        var customTimeProvider = new FixedTimeProvider(FixedNow);
        services.AddSingleton<TimeProvider>(customTimeProvider);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MarketplaceAnalytics:Ebay:OAuth:Enabled"] = "false"
                })
            .Build();

        services.AddEbayAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Same(customTimeProvider, provider.GetRequiredService<TimeProvider>());
    }

    private static EbayAuthenticationService CreateService(
        RecordingHandler? handler = null,
        EbayOAuthOptions? options = null,
        TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient(handler ?? SuccessHandler())
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };

        return new EbayAuthenticationService(
            httpClient,
            Options.Create(options ?? ValidOptions()),
            new EbayEndpointResolver(),
            new FixedTimeProvider(FixedNow));
    }

    private static EbayOAuthOptions ValidOptions()
    {
        return new EbayOAuthOptions
        {
            Enabled = true,
            Environment = EbayEnvironment.Sandbox,
            ClientId = FakeClientId,
            ClientSecret = FakeClientSecret,
            RedirectUriName = FakeRuName,
            DefaultScopes = ["https://api.ebay.com/oauth/api_scope"],
            RequestTimeoutSeconds = 30
        };
    }

    private static EbayOAuthOptions WithEnvironment(
        EbayOAuthOptions options,
        EbayEnvironment environment)
    {
        return new EbayOAuthOptions
        {
            Enabled = options.Enabled,
            Environment = environment,
            ClientId = options.ClientId,
            ClientSecret = options.ClientSecret,
            RedirectUriName = options.RedirectUriName,
            DefaultScopes = options.DefaultScopes,
            RequestTimeoutSeconds = options.RequestTimeoutSeconds
        };
    }

    private static RecordingHandler SuccessHandler()
    {
        return new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                "{\"access_token\":\"FAKE_ACCESS_TOKEN\",\"expires_in\":7200,\"refresh_token\":\"FAKE_REFRESH_TOKEN\",\"refresh_token_expires_in\":86400,\"token_type\":\"Bearer\",\"scope\":\"scope.one scope.two\"}")));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static void AssertTokenRequest(RecordingHandler handler, string grantType)
    {
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.sandbox.ebay.com/identity/v1/oauth2/token", handler.Uri?.AbsoluteUri);
        Assert.Equal("application/x-www-form-urlencoded", handler.ContentType);
        Assert.Contains($"grant_type={grantType}", handler.Body);

        var expectedCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{FakeClientId}:{FakeClientSecret}"));
        Assert.Equal(new AuthenticationHeaderValue("Basic", expectedCredentials), handler.Authorization);
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }

        public Uri? Uri { get; private set; }

        public AuthenticationHeaderValue? Authorization { get; private set; }

        public string? ContentType { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return await responder(request, cancellationToken);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
