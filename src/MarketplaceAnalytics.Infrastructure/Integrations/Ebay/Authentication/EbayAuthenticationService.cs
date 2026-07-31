using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MarketplaceAnalytics.Application.Integrations.Ebay.Authentication;
using Microsoft.Extensions.Options;

namespace MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication;

internal sealed class EbayAuthenticationService(
    HttpClient httpClient,
    IOptions<EbayOAuthOptions> options,
    EbayEndpointResolver endpointResolver,
    TimeProvider timeProvider)
    : IEbayAuthenticationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly EbayOAuthOptions _options = options.Value;

    public Task<Uri> GetUserAuthorizationUriAsync(
        IReadOnlyCollection<string>? scopes = null,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureEnabled();

        var resolvedScopes = ResolveScopes(scopes);
        var endpoint = endpointResolver.Resolve(_options.Environment).AuthorizationEndpoint;
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("client_id", _options.ClientId),
            new("redirect_uri", _options.RedirectUriName),
            new("response_type", "code"),
            new("scope", string.Join(' ', resolvedScopes))
        };

        if (!string.IsNullOrWhiteSpace(state))
        {
            parameters.Add(new KeyValuePair<string, string>("state", state));
        }

        var query = string.Join(
            '&',
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));

        return Task.FromResult(new Uri($"{endpoint}?{query}"));
    }

    public Task<EbayOAuthTokenResult> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode);

        return RequestTokenAsync(
            [
                new("grant_type", "authorization_code"),
                new("code", authorizationCode),
                new("redirect_uri", _options.RedirectUriName)
            ],
            _options.DefaultScopes,
            cancellationToken);
    }

    public Task<EbayOAuthTokenResult> RefreshUserTokenAsync(
        string refreshToken,
        IReadOnlyCollection<string>? scopes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "refresh_token"),
            new("refresh_token", refreshToken)
        };

        IReadOnlyCollection<string> requestedScopes = [];
        if (scopes is not null)
        {
            requestedScopes = ValidateScopes(scopes);
            form.Add(new KeyValuePair<string, string>("scope", string.Join(' ', requestedScopes)));
        }

        return RequestTokenAsync(form, requestedScopes, cancellationToken);
    }

    public Task<EbayOAuthTokenResult> AcquireApplicationTokenAsync(
        IReadOnlyCollection<string>? scopes = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedScopes = ResolveScopes(scopes);

        return RequestTokenAsync(
            [
                new("grant_type", "client_credentials"),
                new("scope", string.Join(' ', resolvedScopes))
            ],
            resolvedScopes,
            cancellationToken);
    }

    private async Task<EbayOAuthTokenResult> RequestTokenAsync(
        IEnumerable<KeyValuePair<string, string>> formValues,
        IReadOnlyCollection<string> requestedScopes,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        var endpoint = endpointResolver.Resolve(_options.Environment).TokenEndpoint;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(formValues)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}")));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException exception)
        {
            throw new EbayAuthenticationException(
                "The eBay OAuth request timed out.",
                "request_timeout",
                exception);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var oauthError = TryDeserializeError(responseBody);
                var errorCode = oauthError?.Error ?? "oauth_request_failed";
                throw new EbayAuthenticationException(
                    $"The eBay OAuth request failed with status {(int)response.StatusCode} ({errorCode}).",
                    errorCode);
            }

            EbayOAuthResponse tokenResponse;
            try
            {
                tokenResponse = JsonSerializer.Deserialize<EbayOAuthResponse>(
                        responseBody,
                        JsonOptions)
                    ?? throw new JsonException("The token response was empty.");
            }
            catch (JsonException exception)
            {
                throw new EbayAuthenticationException(
                    "The eBay OAuth response was not valid JSON.",
                    "invalid_oauth_response",
                    exception);
            }

            return MapTokenResponse(tokenResponse, requestedScopes);
        }
    }

    private EbayOAuthTokenResult MapTokenResponse(
        EbayOAuthResponse response,
        IReadOnlyCollection<string> requestedScopes)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            throw new EbayAuthenticationException(
                "The eBay OAuth response did not contain an access token.",
                "missing_access_token");
        }

        if (response.ExpiresIn <= 0)
        {
            throw new EbayAuthenticationException(
                "The eBay OAuth response contained an invalid access-token lifetime.",
                "invalid_expires_in");
        }

        var now = timeProvider.GetUtcNow();
        var accessToken = new EbayAccessToken(
            response.AccessToken,
            string.IsNullOrWhiteSpace(response.TokenType) ? "Bearer" : response.TokenType,
            now.AddSeconds(response.ExpiresIn));

        EbayRefreshToken? refreshToken = null;
        if (!string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            DateTimeOffset? refreshExpiresAt = response.RefreshTokenExpiresIn is > 0
                ? now.AddSeconds(response.RefreshTokenExpiresIn.Value)
                : null;
            refreshToken = new EbayRefreshToken(response.RefreshToken, refreshExpiresAt);
        }

        var grantedScopes = string.IsNullOrWhiteSpace(response.Scope)
            ? requestedScopes
            : response.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new EbayOAuthTokenResult(accessToken, refreshToken, grantedScopes);
    }

    private static EbayOAuthErrorResponse? TryDeserializeError(string responseBody)
    {
        try
        {
            return JsonSerializer.Deserialize<EbayOAuthErrorResponse>(responseBody, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private IReadOnlyCollection<string> ResolveScopes(IReadOnlyCollection<string>? scopes)
    {
        return ValidateScopes(scopes ?? _options.DefaultScopes);
    }

    private static IReadOnlyCollection<string> ValidateScopes(
        IReadOnlyCollection<string> scopes)
    {
        if (scopes.Count == 0 || scopes.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one non-empty eBay OAuth scope is required.",
                nameof(scopes));
        }

        return scopes;
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
        {
            throw new EbayAuthenticationException(
                "eBay authentication is disabled.",
                "integration_disabled");
        }
    }
}
