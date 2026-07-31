# eBay APIs Foundation

## Purpose and phase boundary

Prompt 007 provides one-page, read-only access foundations for selected eBay Sell
APIs. It does not persist responses, synchronize history, schedule work, mutate eBay
resources, calculate business values, or make real Sandbox or Production calls.

## Supported APIs and operations

| API | Operations |
| --- | --- |
| Inventory API v1 | Get an inventory-items page; get one inventory item by SKU |
| Fulfillment API v1 | Get an orders page; get one order by order ID |
| Finances API v1 | Get a transactions page; get a payouts page |

Application owns client interfaces, typed queries, pagination, money, errors, and
normalized response records. Infrastructure owns endpoint paths, `HttpClient`, query
encoding, bearer headers, JSON transport DTOs, deserialization, mapping, and eBay
error parsing. Domain contains no remote eBay documents or transport concerns.

## Authentication reuse and scopes

Every client obtains user tokens exclusively through `IEbayUserAccessTokenProvider`.
Clients supply only their operation's required scope and never read refresh-token
configuration, exchange refresh tokens, or implement token caching.

| API | Required scope |
| --- | --- |
| Inventory | `https://api.ebay.com/oauth/api_scope/sell.inventory.readonly` |
| Fulfillment | `https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly` |
| Finances | `https://api.ebay.com/oauth/api_scope/sell.finances` |

The bearer token is attached to each new request message. Shared `HttpClient` default
authorization headers are not mutated, and tokens are never logged or included in
exceptions.

## Environments and HTTP lifetime

The existing validated OAuth environment selects the API base address:

- Sandbox: `https://api.sandbox.ebay.com/`
- Production: `https://api.ebay.com/`

All clients are registered as typed clients through `IHttpClientFactory`. Automated
tests replace HTTP transport and tokens with in-memory fakes; no DNS or eBay connection
is required or attempted.

## Pagination

`EbayPageRequest` requires `limit > 0` and `offset >= 0`. Each method requests exactly
one bounded page and returns optional total, limit, offset, next, and previous values
when eBay supplies them. It never follows pagination links automatically. Prompt 008
can use this boundary to control checkpoints, rate limits, memory, persistence, and
resumption without changing the clients.

## Filters and UTC dates

Typed query records accept date ranges and external status/type strings. Reverse date
ranges are rejected before transmission. Infrastructure builds filters from fixed
field names, formats timestamps as invariant UTC ISO-8601 values, orders parameters
deterministically, and URI-encodes each query or path value exactly once. Callers cannot
inject raw filter fragments.

## Money and external values

Amounts use `decimal` and remain paired with their currency code. External identifiers
and seller SKUs are preserved. Status and type values remain strings so newly introduced
eBay values do not break deserialization. Missing optional data is not invented.

## Errors and cancellation

`EbayApiException` preserves the numeric HTTP status, operation, multiple structured
eBay errors, safe request identifier, and `Retry-After` when available. This keeps 401,
403, 404, 429, and 5xx outcomes distinguishable. Empty or malformed error bodies use a
safe fallback that excludes response content. Successful malformed JSON fails explicitly.

Cancellation propagates through token retrieval, HTTP transmission, and response
reading. `OperationCanceledException` remains cancellation. No retries are performed.

## Sensitive data and logging

Clients do not log access tokens, refresh tokens, authorization headers, response
bodies, buyer names, addresses, email addresses, or telephone numbers. Buyer and
shipping-address transport data is deliberately not mapped because Prompt 008 has no
approved need for it. Tracked configuration contains no credentials or tokens.

## Known eBay limitations

### Inventory visibility

The Inventory API exposes inventory represented in eBay's Inventory API model. Existing
listings not created through or migrated into that model may not automatically appear.
This is an integration risk for later validation and is not solved in Prompt 007.

### Fulfillment completed checkout

Fulfillment `getOrders` represents completed-checkout orders. Purchases still waiting
for required upfront payment are not represented as completed-checkout orders.

### Finances account access

Financial information is returned for the authenticated user subject to eBay permissions
and account access. Team Access is not assumed.

## Deferred functionality

Prompt 008 may ingest the normalized one-page results. PostgreSQL persistence, migrations,
repositories, checkpoints, synchronization, workers, retries, calculations, reporting,
write operations, webhooks, and real connectivity verification remain explicitly deferred.
