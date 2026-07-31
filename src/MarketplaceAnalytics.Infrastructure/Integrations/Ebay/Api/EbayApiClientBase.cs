using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MarketplaceAnalytics.Application.Integrations.Ebay.Api;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api.Transport;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Api;

internal abstract class EbayApiClientBase(HttpClient httpClient, IEbayUserAccessTokenProvider tokenProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    protected async Task<T> GetAsync<T>(string requestUri, IReadOnlyCollection<string> scopes, string operation, CancellationToken cancellationToken)
        where T : class
    {
        var result = await GetCoreAsync<T>(requestUri, scopes, operation, false, cancellationToken);
        return result!;
    }

    protected Task<T?> GetOptionalAsync<T>(string requestUri, IReadOnlyCollection<string> scopes, string operation, CancellationToken cancellationToken)
        where T : class => GetCoreAsync<T>(requestUri, scopes, operation, true, cancellationToken);

    private async Task<T?> GetCoreAsync<T>(string requestUri, IReadOnlyCollection<string> scopes, string operation, bool allowNotFound, CancellationToken cancellationToken)
        where T : class
    {
        var accessToken = await tokenProvider.GetAccessTokenAsync(scopes, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw CreateApiException(response, body, operation);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions)
                ?? throw new JsonException("The response body was empty.");
        }
        catch (JsonException exception)
        {
            throw new EbayApiException("The eBay API response was not valid JSON.", operation, (int)response.StatusCode, innerException: exception);
        }
    }

    private static EbayApiException CreateApiException(HttpResponseMessage response, string body, string operation)
    {
        IReadOnlyList<EbayApiError> errors = [];
        try
        {
            var envelope = JsonSerializer.Deserialize<EbayErrorEnvelopeDto>(body, JsonOptions);
            errors = envelope?.Errors?.Select(error => new EbayApiError(error.ErrorId, error.Domain, error.Category, error.Subdomain, error.Message, error.LongMessage)).ToArray() ?? [];
        }
        catch (JsonException)
        {
            // The safe fallback below intentionally excludes the response body.
        }

        var requestId = GetHeader(response, "X-EBAY-C-REQUEST-ID") ?? GetHeader(response, "X-EBAY-CORRELATION-ID");
        var retryAfter = response.Headers.RetryAfter?.Delta;
        if (retryAfter is null && response.Headers.RetryAfter?.Date is DateTimeOffset retryDate)
        {
            retryAfter = retryDate - DateTimeOffset.UtcNow;
        }

        return new EbayApiException($"The eBay API operation '{operation}' failed with HTTP status {(int)response.StatusCode}.", operation, (int)response.StatusCode, errors, requestId, retryAfter);
    }

    private static string? GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    protected static Uri? ToUri(string? value) => Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    protected static EbayMoney? ToMoney(EbayMoneyDto? value) => value?.Currency is null || value.Value is null ? null : new EbayMoney(value.Value.Value, value.Currency);

    protected static string RequiredIdentifier(string? value, string name, string operation)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new EbayApiException($"The eBay API response omitted the required {name}.", operation, 200);
        }

        return value;
    }
}
