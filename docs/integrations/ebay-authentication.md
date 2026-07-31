# eBay Authentication Foundation

## Purpose and phase boundary

Prompt 006 provides OAuth 2.0 authentication contracts and Infrastructure transport
for future eBay clients. It supports Authorization Code, Refresh Token, and Client
Credentials grants. It does not call Inventory, Fulfillment, Finances, or other eBay
business APIs; those begin in Prompt 007.

Stable normalized token contracts live in
`MarketplaceAnalytics.Application.Integrations.Ebay.Authentication`. Configuration,
endpoint selection, HTTP requests, Basic authentication, JSON DTOs, and response
mapping live in `MarketplaceAnalytics.Infrastructure.Integrations.Ebay.Authentication`.
Domain contains no eBay concerns.

## Environments and endpoints

Exactly two environments are supported:

| Environment | Authorization endpoint | Token endpoint |
| --- | --- | --- |
| Sandbox | `https://auth.sandbox.ebay.com/oauth2/authorize` | `https://api.sandbox.ebay.com/identity/v1/oauth2/token` |
| Production | `https://auth.ebay.com/oauth2/authorize` | `https://api.ebay.com/identity/v1/oauth2/token` |

Use Sandbox credentials and RuName with Sandbox. Production values are separate and
must be enabled only after owner approval.

## Required eBay Developer Program values

From the eBay Developer Program application keys and user-token settings, obtain:

- Client ID, also called App ID
- Client Secret, also called Cert ID
- Redirect URI name, also called RuName
- Approved OAuth scopes required by the later client

The Redirect URI value is eBay's registered RuName. It is not an arbitrary localhost
callback URL.

## Configuration

Section:

```text
MarketplaceAnalytics:Ebay:OAuth
```

Tracked configuration keeps the integration disabled with empty credentials. When
enabled, startup requires a supported environment, ClientId, ClientSecret, RuName,
at least one non-empty scope, and a timeout from 1 through 300 seconds.

### User Secrets

Use clearly local values and never commit the output:

```powershell
dotnet user-secrets set "MarketplaceAnalytics:Ebay:OAuth:Enabled" "true" --project .\src\MarketplaceAnalytics.API
dotnet user-secrets set "MarketplaceAnalytics:Ebay:OAuth:Environment" "Sandbox" --project .\src\MarketplaceAnalytics.API
dotnet user-secrets set "MarketplaceAnalytics:Ebay:OAuth:ClientId" "<sandbox-app-id>" --project .\src\MarketplaceAnalytics.API
dotnet user-secrets set "MarketplaceAnalytics:Ebay:OAuth:ClientSecret" "<sandbox-cert-id>" --project .\src\MarketplaceAnalytics.API
dotnet user-secrets set "MarketplaceAnalytics:Ebay:OAuth:RedirectUriName" "<sandbox-runame>" --project .\src\MarketplaceAnalytics.API
dotnet user-secrets set "MarketplaceAnalytics:Ebay:OAuth:DefaultScopes:0" "https://api.ebay.com/oauth/api_scope" --project .\src\MarketplaceAnalytics.API
```

### Environment variables

The standard double-underscore hierarchy is supported:

```text
MarketplaceAnalytics__Ebay__OAuth__Enabled
MarketplaceAnalytics__Ebay__OAuth__Environment
MarketplaceAnalytics__Ebay__OAuth__ClientId
MarketplaceAnalytics__Ebay__OAuth__ClientSecret
MarketplaceAnalytics__Ebay__OAuth__RedirectUriName
MarketplaceAnalytics__Ebay__OAuth__DefaultScopes__0
MarketplaceAnalytics__Ebay__OAuth__RequestTimeoutSeconds
```

## Authorization URI generation

Resolve `IEbayAuthenticationService` and call
`GetUserAuthorizationUriAsync`. The result includes `client_id`, the registered
RuName as `redirect_uri`, `response_type=code`, URL-encoded scopes, and optional
state. Present that URI to the user outside the API; Prompt 006 intentionally exposes
no production HTTP endpoint for tokens or authorization codes.

Use a cryptographically unpredictable state value in a real interactive flow and
verify it when handling the returned authorization response. Callback orchestration
belongs to the consuming application flow, not this foundation.

## Grant flows

### Authorization Code

`ExchangeAuthorizationCodeAsync` posts `grant_type=authorization_code`, the short-lived
code, and the registered RuName. The code must never be logged or persisted.

### Refresh Token

`RefreshUserTokenAsync` posts `grant_type=refresh_token` and the refresh token. An
optional explicit scope set may be supplied. No token store is implemented.

### Client Credentials

`AcquireApplicationTokenAsync` posts `grant_type=client_credentials` and scopes to
obtain an application access token.

All token requests use form-encoded bodies and UTF-8/Base64 HTTP Basic credentials.
Internal eBay DTOs are mapped to immutable Application models.

## Token expiration policy

Absolute expiration is calculated with injected `TimeProvider`:

```text
ExpiresAtUtc = TimeProvider.GetUtcNow() + expires_in
```

An access token becomes unusable during the final one-minute safety window. Callers
must refresh or reacquire it rather than waiting for exact expiry. Tokens remain only
in caller-controlled memory in Prompt 006.

## Secret handling

Never log, commit, persist to files, or place in exception messages:

- Client Secret
- HTTP Authorization header
- Authorization code
- Access token
- Refresh token
- Complete OAuth response body

Exceptions expose safe operation/status/error identifiers only. No PostgreSQL token
persistence exists in this phase.

## Testing

Automated tests use fake credentials, deterministic `TimeProvider`, and an in-memory
`HttpMessageHandler`. They verify complete request shape and response mapping without
DNS, network access, or calls to eBay.

Run:

```powershell
dotnet test .\MarketplaceAnalytics.sln --configuration Release
```

## Troubleshooting

- `invalid_client`: verify environment-specific App ID and Cert ID.
- `invalid_grant`: verify the authorization code is unused and unexpired, the RuName
  matches, and Sandbox/Production values are not mixed.
- `invalid_scope`: verify spelling and approval of every configured scope.
- Startup validation failure: either keep `Enabled=false` or configure every required
  value securely.
- Request timeout: verify local connectivity and the selected eBay environment; do not
  increase the timeout beyond 300 seconds.

Prompt 007 implements actual eBay API clients. Prompt 006 must not be expanded into
business data synchronization or multi-account orchestration.
