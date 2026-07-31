# ADR-0002: PostgreSQL as the Primary Relational Database

## Status

Accepted

## Context

MarketplaceAnalytics needs a durable relational persistence foundation that supports
local-first development, deterministic schema migrations, mature .NET tooling, and
future analytical workloads without introducing paid or cloud dependencies.

## Decision

Use PostgreSQL Community as the primary relational database. Keep Entity Framework
Core and Npgsql implementation details in `MarketplaceAnalytics.Infrastructure`, with
the API acting only as the composition root.

## Reasons

- PostgreSQL Community is free and open source.
- It runs locally on supported developer operating systems.
- Npgsql provides a mature open-source EF Core provider.
- PostgreSQL supports transactional relational workloads and future analytical needs.
- EF Core migrations provide deterministic, reviewable schema evolution.
- Standard connection strings and environment variables preserve deployment freedom.

## Alternatives considered

### SQL Server

Rejected as the primary choice because PostgreSQL better matches the fixed local-first,
free-tools requirement without edition or licensing considerations.

### SQLite

Rejected as the primary database because its concurrency, type, schema, and operational
behavior differ materially from the planned PostgreSQL deployment target.

### Cloud-managed database

Rejected as a foundation requirement because it would introduce network, account,
vendor, and potentially paid-service dependencies.

### Database-agnostic provider abstraction

Rejected for this phase as speculative complexity. Clean Architecture already isolates
the provider in Infrastructure; a second abstraction is unnecessary without a concrete
business requirement.

## Consequences

- Local PostgreSQL Community is the reference implementation for live verification.
- Infrastructure owns DbContext, provider configuration, migrations, and connectivity
  health checking.
- Domain and Application remain independent of EF Core and Npgsql.
- Developers must supply credentials outside source control.
- Provider-specific migrations require review if the database platform changes.

## Local First compatibility

The database, EF tool, migrations, configuration, and tests can all operate locally.
Docker, cloud hosting, and paid services are optional future deployment choices, not
development prerequisites. Tests that verify registration remain database-independent.

## Future migration compatibility

Provider isolation in Infrastructure prevents PostgreSQL details from entering Domain
or Application. A future database migration would require a new Infrastructure provider
and schema migration plan, but not a redesign of the application core. Standard
connection configuration also remains compatible with VPS, container, CI/CD, or cloud
environments when their roadmap phases begin.
