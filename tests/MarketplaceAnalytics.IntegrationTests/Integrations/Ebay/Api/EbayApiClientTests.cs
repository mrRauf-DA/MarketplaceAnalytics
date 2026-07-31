using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Finances;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Fulfillment;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api.Inventory;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Finances;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Fulfillment;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Inventory;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MarketplaceAnalytics.IntegrationTests.Integrations.Ebay.Api;

public sealed class EbayApiClientTests
{
    [Fact]
    public async Task InventoryPage_UsesPaginationScopeBearerAndMapsOptionalData()
    {
        const string json = """
            {"total":1,"limit":25,"offset":5,"next":"/next","unknown":"ignored","inventoryItems":[{"sku":"SKU-1","locale":"en_US","condition":"NEW","product":{"title":"Title","description":"Description","aspects":{"Brand":["Example"]}},"availability":{"shipToLocationAvailability":{"quantity":7}},"packageWeightAndSize":{"weight":{"value":2.5,"unit":"POUND"},"dimensions":{"height":1,"length":3,"width":2,"unit":"INCH"},"packageType":"BOX"}}]}
            """;
        var handler = Handler(HttpStatusCode.OK, json);
        var tokens = new StubTokenProvider();
        var client = new EbayInventoryClient(Client(handler), tokens);

        var result = await client.GetInventoryItemsAsync(new EbayPageRequest(25, 5));

        Assert.Equal("https://api.sandbox.ebay.com/sell/inventory/v1/inventory_item?limit=25&offset=5", handler.Uri?.AbsoluteUri);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal(InventoryScope, Assert.Single(tokens.Scopes));
        var item = Assert.Single(result.Items);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(7, item.AvailableQuantity);
        Assert.Equal(2.5m, item.Package?.Weight);
        Assert.Equal("Example", Assert.Single(item.Aspects["Brand"]));
    }

    [Fact]
    public async Task InventoryPage_AllowsEmptyItemsAndMissingPaginationProperties()
    {
        var client = new EbayInventoryClient(Client(Handler(HttpStatusCode.OK, "{\"inventoryItems\":[]}")), new StubTokenProvider());

        var result = await client.GetInventoryItemsAsync(new EbayPageRequest(10, 0));

        Assert.Empty(result.Items);
        Assert.Null(result.Total);
        Assert.Null(result.Next);
    }

    [Fact]
    public async Task InventoryItem_EncodesSkuAndPreservesSellerSku()
    {
        var handler = Handler(HttpStatusCode.OK, "{\"sku\":\"seller sku/ä\"}");
        var client = new EbayInventoryClient(Client(handler), new StubTokenProvider());

        var result = await client.GetInventoryItemAsync("seller sku/ä");

        Assert.Contains("seller%20sku%2F%C3%A4", handler.Uri?.AbsoluteUri);
        Assert.Equal("seller sku/ä", result?.Sku);
    }

    [Fact]
    public async Task InventoryItem_RejectsEmptySkuWithoutTransmission()
    {
        var handler = Handler(HttpStatusCode.OK, "{}");
        var client = new EbayInventoryClient(Client(handler), new StubTokenProvider());

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetInventoryItemAsync(" "));

        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task InventoryItem_ReturnsNullForNotFound()
    {
        var client = new EbayInventoryClient(Client(Handler(HttpStatusCode.NotFound, "")), new StubTokenProvider());

        Assert.Null(await client.GetInventoryItemAsync("missing"));
    }

    [Fact]
    public void Pagination_RejectsInvalidInputBeforeAnyClientCall()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EbayPageRequest(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EbayPageRequest(1, -1));
    }

    [Fact]
    public async Task MalformedError_UsesSafeFallbackWithoutTokenValue()
    {
        var client = new EbayInventoryClient(Client(Handler(HttpStatusCode.InternalServerError, "not-json")), new StubTokenProvider());

        var exception = await Assert.ThrowsAsync<EbayApiException>(() => client.GetInventoryItemsAsync(new EbayPageRequest(10, 0)));

        Assert.Equal(500, exception.StatusCode);
        Assert.Empty(exception.Errors);
        Assert.DoesNotContain(TestAccessToken, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Orders_EncodeUtcFiltersAndMapLineItemMoneyAndExternalValues()
    {
        const string json = """
            {"total":1,"limit":20,"offset":2,"orders":[{"orderId":"ORDER-1","orderFulfillmentStatus":"FUTURE_STATUS","creationDate":"2026-01-01T00:00:00Z","pricingSummary":{"total":{"value":12.34,"currency":"USD"}},"lineItems":[{"lineItemId":"LINE-1","legacyItemId":"LEGACY-1","sku":"SKU-1","quantity":2,"lineItemCost":{"value":10.00,"currency":"USD"}}]}]}
            """;
        var handler = Handler(HttpStatusCode.OK, json);
        var tokens = new StubTokenProvider();
        var client = new EbayFulfillmentClient(Client(handler), tokens);
        var query = new EbayOrderQuery(new EbayPageRequest(20, 2), new EbayDateRange(new DateTimeOffset(2026, 1, 1, 4, 0, 0, TimeSpan.FromHours(4)), new DateTimeOffset(2026, 1, 2, 4, 0, 0, TimeSpan.FromHours(4))), new EbayDateRange(DateTimeOffset.Parse("2026-01-03T00:00:00Z"), DateTimeOffset.Parse("2026-01-04T00:00:00Z")), "NOT_STARTED");

        var result = await client.GetOrdersAsync(query);

        Assert.StartsWith("https://api.sandbox.ebay.com/sell/fulfillment/v1/order?limit=20&offset=2&filter=", handler.Uri?.AbsoluteUri);
        Assert.Contains("creationdate%3A%5B2026-01-01T00%3A00%3A00.000Z..2026-01-02T00%3A00%3A00.000Z%5D", handler.Uri?.AbsoluteUri);
        Assert.Contains("orderfulfillmentstatus%3A%7BNOT_STARTED%7D", handler.Uri?.AbsoluteUri);
        Assert.Equal(FulfillmentScope, Assert.Single(tokens.Scopes));
        var order = Assert.Single(result.Items);
        Assert.Equal("FUTURE_STATUS", order.OrderFulfillmentStatus);
        Assert.Equal(12.34m, order.Total?.Amount);
        var line = Assert.Single(order.LineItems);
        Assert.Equal("LEGACY-1", line.LegacyItemId);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal("USD", line.LineItemCost?.Currency);
    }

    [Fact]
    public async Task Order_EncodesIdAndReturnsNullForNotFound()
    {
        var handler = Handler(HttpStatusCode.NotFound, "");
        var client = new EbayFulfillmentClient(Client(handler), new StubTokenProvider());

        var result = await client.GetOrderAsync("order/id value");

        Assert.Null(result);
        Assert.Contains("order%2Fid%20value", handler.Uri?.AbsoluteUri);
    }

    [Fact]
    public async Task Order_MapsSuccessfulSingleResource()
    {
        var client = new EbayFulfillmentClient(
            Client(Handler(HttpStatusCode.OK, "{\"orderId\":\"ORDER-2\",\"lineItems\":[]}")),
            new StubTokenProvider());

        var result = await client.GetOrderAsync("ORDER-2");

        Assert.Equal("ORDER-2", result?.OrderId);
        Assert.Empty(result?.LineItems ?? []);
    }

    [Fact]
    public async Task Order_RejectsEmptyIdWithoutTransmission()
    {
        var handler = Handler(HttpStatusCode.OK, "{}");
        var client = new EbayFulfillmentClient(Client(handler), new StubTokenProvider());

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetOrderAsync(""));
        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public void DateRange_RejectsReverseRange()
    {
        Assert.Throws<ArgumentException>(() => new EbayDateRange(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task UnsafeFilterValue_IsRejectedWithoutTransmission()
    {
        var handler = Handler(HttpStatusCode.OK, "{}");
        var client = new EbayFulfillmentClient(Client(handler), new StubTokenProvider());
        var query = new EbayOrderQuery(new EbayPageRequest(10, 0), FulfillmentStatus: "NOT_STARTED},creationdate:[unsafe]");

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetOrdersAsync(query));

        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task FulfillmentCancellation_ReachesHttpOperation()
    {
        var transmissionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            transmissionStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Response(HttpStatusCode.OK, "{}");
        });
        var client = new EbayFulfillmentClient(Client(handler), new StubTokenProvider());
        using var cancellation = new CancellationTokenSource();

        var operation = client.GetOrdersAsync(new EbayOrderQuery(new EbayPageRequest(1, 0)), cancellation.Token);
        await transmissionStarted.Task;
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

    [Fact]
    public async Task Transactions_EncodeFiltersAndPreserveIdentifiersFeesMoneyAndUnknownStrings()
    {
        const string json = """
            {"transactions":[{"transactionId":"TX-1","transactionType":"FUTURE_TYPE","transactionStatus":"FUTURE_STATUS","orderId":"ORDER-1","lineItemId":"LINE-1","payoutId":"PAYOUT-1","bookingEntry":"CREDIT","transactionDate":"2026-02-01T00:00:00Z","amount":{"value":50.25,"currency":"EUR"},"totalFeeBasisAmount":{"value":50.25,"currency":"EUR"},"totalFeeAmount":{"value":5.10,"currency":"EUR"},"orderLineItems":[{"lineItemId":"LINE-1","feeBasisAmount":{"value":50.25,"currency":"EUR"}}]}]}
            """;
        var handler = Handler(HttpStatusCode.OK, json);
        var tokens = new StubTokenProvider();
        var client = new EbayFinancesClient(Client(handler), tokens);
        var query = new EbayTransactionQuery(new EbayPageRequest(50, 10), new EbayDateRange(DateTimeOffset.Parse("2026-02-01T00:00:00Z"), DateTimeOffset.Parse("2026-02-02T00:00:00Z")), "FUNDS_AVAILABLE_FOR_PAYOUT", "SALE");

        var result = await client.GetTransactionsAsync(query);

        Assert.StartsWith("https://api.sandbox.ebay.com/sell/finances/v1/transaction?limit=50&offset=10&filter=", handler.Uri?.AbsoluteUri);
        Assert.Contains("transactionStatus%3A%7BFUNDS_AVAILABLE_FOR_PAYOUT%7D", handler.Uri?.AbsoluteUri);
        Assert.Equal(FinancesScope, Assert.Single(tokens.Scopes));
        var transaction = Assert.Single(result.Items);
        Assert.Equal("TX-1", transaction.TransactionId);
        Assert.Equal("ORDER-1", transaction.OrderId);
        Assert.Equal("LINE-1", transaction.LineItemId);
        Assert.Equal("PAYOUT-1", transaction.PayoutId);
        Assert.Equal("FUTURE_TYPE", transaction.TransactionType);
        Assert.Equal(50.25m, transaction.Amount?.Amount);
        Assert.Equal(5.10m, transaction.TotalFeeAmount?.Amount);
        Assert.Single(transaction.OrderLineItems);
    }

    [Fact]
    public async Task Payouts_EncodeFiltersAndPreserveUnknownStatusMoneyAndId()
    {
        const string json = "{\"payouts\":[{\"payoutId\":\"P-1\",\"payoutStatus\":\"FUTURE_STATUS\",\"amount\":{\"value\":99.5,\"currency\":\"GBP\"},\"payoutDate\":\"2026-03-01T00:00:00Z\",\"transactionCount\":4}]}";
        var handler = Handler(HttpStatusCode.OK, json);
        var tokens = new StubTokenProvider();
        var client = new EbayFinancesClient(Client(handler), tokens);
        var query = new EbayPayoutQuery(new EbayPageRequest(10, 0), new EbayDateRange(DateTimeOffset.Parse("2026-03-01T00:00:00Z"), DateTimeOffset.Parse("2026-03-02T00:00:00Z")), "SUCCEEDED");

        var result = await client.GetPayoutsAsync(query);

        Assert.Contains("payoutStatus%3A%7BSUCCEEDED%7D", handler.Uri?.AbsoluteUri);
        Assert.Equal(FinancesScope, Assert.Single(tokens.Scopes));
        var payout = Assert.Single(result.Items);
        Assert.Equal("P-1", payout.PayoutId);
        Assert.Equal("FUTURE_STATUS", payout.PayoutStatus);
        Assert.Equal("GBP", payout.Amount?.Currency);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.Forbidden, 403)]
    public async Task StructuredAuthorizationErrorsRemainDistinguishable(HttpStatusCode status, int expected)
    {
        const string json = "{\"errors\":[{\"errorId\":1001,\"domain\":\"ACCESS\",\"category\":\"REQUEST\",\"subdomain\":\"AUTH\",\"message\":\"Denied\",\"longMessage\":\"Not authorized\"},{\"errorId\":1002,\"message\":\"Second\"}]}";
        var client = new EbayFinancesClient(Client(Handler(status, json)), new StubTokenProvider());

        var exception = await Assert.ThrowsAsync<EbayApiException>(() => client.GetTransactionsAsync(new EbayTransactionQuery(new EbayPageRequest(10, 0))));

        Assert.Equal(expected, exception.StatusCode);
        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal(1001, exception.Errors[0].ErrorId);
    }

    [Fact]
    public async Task RateLimitErrorPreservesRetryAfterAndRequestId()
    {
        var handler = new RecordingHandler((_, _) =>
        {
            var response = Response(HttpStatusCode.TooManyRequests, "{}");
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            response.Headers.Add("X-EBAY-C-REQUEST-ID", "request-1");
            return Task.FromResult(response);
        });
        var client = new EbayFinancesClient(Client(handler), new StubTokenProvider());

        var exception = await Assert.ThrowsAsync<EbayApiException>(() => client.GetPayoutsAsync(new EbayPayoutQuery(new EbayPageRequest(10, 0))));

        Assert.Equal(429, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), exception.RetryAfter);
        Assert.Equal("request-1", exception.RequestId);
    }

    [Fact]
    public async Task MalformedSuccessJsonFailsExplicitlyAndSafely()
    {
        var client = new EbayFinancesClient(Client(Handler(HttpStatusCode.OK, "not-json")), new StubTokenProvider());

        var exception = await Assert.ThrowsAsync<EbayApiException>(() => client.GetPayoutsAsync(new EbayPayoutQuery(new EbayPageRequest(10, 0))));

        Assert.Equal(200, exception.StatusCode);
        Assert.Equal("GetPayouts", exception.Operation);
    }

    [Fact]
    public void EndpointResolver_SelectsSandboxAndProductionApiBaseAddresses()
    {
        var resolver = new EbayEndpointResolver();

        Assert.Equal("https://api.sandbox.ebay.com/", resolver.Resolve(EbayEnvironment.Sandbox).ApiBaseAddress.AbsoluteUri);
        Assert.Equal("https://api.ebay.com/", resolver.Resolve(EbayEnvironment.Production).ApiBaseAddress.AbsoluteUri);
    }

    [Fact]
    public void DependencyInjection_ResolvesAllTypedApiClients()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MarketplaceAnalytics:Ebay:OAuth:Enabled"] = "false", ["MarketplaceAnalytics:Ebay:OAuth:Environment"] = "Sandbox", ["MarketplaceAnalytics:Ebay:OAuth:RequestTimeoutSeconds"] = "30" }).Build();
        services.AddEbayAuthentication(configuration);
        services.AddEbayApiClients();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEbayInventoryClient>());
        Assert.NotNull(provider.GetRequiredService<IEbayFulfillmentClient>());
        Assert.NotNull(provider.GetRequiredService<IEbayFinancesClient>());
    }

    private const string InventoryScope = "https://api.ebay.com/oauth/api_scope/sell.inventory.readonly";
    private const string FulfillmentScope = "https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly";
    private const string FinancesScope = "https://api.ebay.com/oauth/api_scope/sell.finances";
    private const string TestAccessToken = "FAKE_API_ACCESS_TOKEN";

    private static HttpClient Client(RecordingHandler handler) => new(handler) { BaseAddress = new Uri("https://api.sandbox.ebay.com/") };
    private static RecordingHandler Handler(HttpStatusCode status, string body) => new((_, _) => Task.FromResult(Response(status, body)));
    private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubTokenProvider : IEbayUserAccessTokenProvider
    {
        public List<string> Scopes { get; } = [];
        public Task<EbayAccessToken> GetAccessTokenAsync(IReadOnlyCollection<string> requiredScopes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Scopes.AddRange(requiredScopes);
            return Task.FromResult(new EbayAccessToken(TestAccessToken, "Bearer", DateTimeOffset.UtcNow.AddHours(1)));
        }
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public int SendCount { get; private set; }
        public Uri? Uri { get; private set; }
        public AuthenticationHeaderValue? Authorization { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization;
            return await responder(request, cancellationToken);
        }
    }
}
