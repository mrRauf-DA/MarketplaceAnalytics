# ADR-0003: eBay OAuth Application Boundary

## Status

Accepted

## Context

Later eBay API clients require renewable user and application tokens, but HTTP,
configuration, eBay response DTOs, credentials, and endpoint selection must not leak
into Domain or application use cases.

## Decision

Keep normalized immutable authentication contracts in Application and implement eBay
OAuth transport in Infrastructure. API performs dependency registration only. Use
`IEbayAuthenticationService` as the reusable boundary for later eBay clients.

The integration is explicitly disabled by default. Enabling it activates strict startup
validation. Token expiry is calculated using injected `TimeProvider`, with a centralized
one-minute usability safety window.

## Consequences

- Application consumers do not depend on eBay HTTP DTOs or Infrastructure.
- OAuth transport and endpoints can change without affecting Domain.
- Tests substitute an in-memory HTTP handler and deterministic clock.
- Tokens remain caller-controlled and are not persisted in Prompt 006.
- API startup remains possible for development that intentionally disables eBay.

## Alternatives rejected

- Putting OAuth contracts in Domain: rejected because OAuth is an external integration.
- Returning eBay response DTOs: rejected because it couples Application to transport.
- Static token cache: rejected because it creates mutable global secret state.
- Database token storage now: rejected as outside Prompt 006.
- Public API token endpoints: rejected because they increase credential exposure and are
  unnecessary for this foundation.

## Future compatibility

Prompt 007 clients can depend on `IEbayAuthenticationService` without reimplementing
OAuth. A later approved phase may add encrypted multi-account token persistence behind
new application contracts without changing Domain or exposing transport DTOs.
