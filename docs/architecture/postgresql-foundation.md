# PostgreSQL Foundation

## Purpose and scope

Prompt 005 establishes the PostgreSQL persistence boundary for MarketplaceAnalytics.
It provides provider registration, a DbContext, migrations, health checking, design-time
tooling, and isolated registration tests. It does not introduce business-domain tables,
repositories, ingestion, reporting, eBay integration, or background processing.

## Clean Architecture placement

All database implementation lives in `MarketplaceAnalytics.Infrastructure.Persistence`.
Domain and Application do not reference Entity Framework Core, Npgsql, connection
strings, or database configuration. The API composition root calls
`AddMarketplaceAnalyticsPersistence`; provider details remain inside Infrastructure.

## Packages and tools

| Dependency | Version | Purpose |
| --- | --- | --- |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4 | EF Core PostgreSQL provider |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.1 | Design-time migration support; private asset |
| `Microsoft.Extensions.Configuration.Json` | 9.0.18 | Design-time appsettings loading |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | 9.0.18 | Design-time environment overrides |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | 9.0.18 | Standard health-check registration |
| `dotnet-ef` local tool | 9.0.1 | Deterministic EF migration commands |

EF Design and `dotnet-ef` use 9.0.1 to match the EF Core line required by Npgsql
9.0.4 and avoid assembly-version conflicts. No third-party naming or health-check
package is used.

## DbContext and schema ownership

`MarketplaceAnalyticsDbContext` is owned by Infrastructure. It deliberately uses:

- Default schema: `marketplace_analytics`
- Migration history table: `marketplace_analytics.__ef_migrations_history`
- Migrations assembly: `MarketplaceAnalytics.Infrastructure`
- Central snake_case table, column, key, index, and foreign-key naming
- UTC conversion for future `DateTime` properties
- Separate `IEntityTypeConfiguration<T>` mappings

The initial migration contains only `database_foundation_marker`, an explicitly
Infrastructure-owned marker used to establish and verify the schema. It is not a
business entity and must not acquire business fields.

## Connection string

The required key is:

```text
ConnectionStrings:MarketplaceAnalyticsDatabase
```

The standard environment-variable form is:

```text
ConnectionStrings__MarketplaceAnalyticsDatabase
```

Tracked appsettings contain only a local placeholder with `CHANGE_ME` credentials.
Replace it outside source control. Missing or blank configuration fails registration
with a clear error. Connection strings and credentials must never be logged.

### PowerShell environment override

```powershell
$env:ConnectionStrings__MarketplaceAnalyticsDatabase = "Host=localhost;Port=5432;Database=marketplace_analytics;Username=<local-user>;Password=<local-password>"
```

Remove the process-local value when finished:

```powershell
Remove-Item Env:ConnectionStrings__MarketplaceAnalyticsDatabase
```

### .NET User Secrets for API runtime

```powershell
dotnet user-secrets set `
  "ConnectionStrings:MarketplaceAnalyticsDatabase" `
  "Host=localhost;Port=5432;Database=marketplace_analytics;Username=<local-user>;Password=<local-password>" `
  --project .\src\MarketplaceAnalytics.API\MarketplaceAnalytics.API.csproj
```

User Secrets support local API execution. Design-time migration commands use the
environment variable above so the Infrastructure factory remains independent of
API secret storage.

## Local PostgreSQL prerequisites

Use a locally installed PostgreSQL Community server. Docker and cloud services are
not required. Create a dedicated development database with your locally approved
role and PostgreSQL tooling; do not reuse, delete, reset, or drop an unknown database.

Example database creation, after verifying the target server and role:

```powershell
createdb --host localhost --port 5432 --username <local-user> marketplace_analytics
```

## Migration commands

Run from the repository root. Restore the pinned tool first:

```powershell
dotnet tool restore
```

Create a migration:

```powershell
dotnet ef migrations add <MigrationName> `
  --project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --startup-project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --output-dir Persistence\Migrations `
  --context MarketplaceAnalyticsDbContext
```

Apply migrations only after verifying the environment variable and database target:

```powershell
dotnet ef database update `
  --project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --startup-project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --context MarketplaceAnalyticsDbContext
```

Remove only the latest unapplied migration:

```powershell
dotnet ef migrations remove `
  --project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --startup-project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --context MarketplaceAnalyticsDbContext
```

Generate an idempotent script without connecting to PostgreSQL:

```powershell
dotnet ef migrations script --idempotent `
  --project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --startup-project .\src\MarketplaceAnalytics.Infrastructure\MarketplaceAnalytics.Infrastructure.csproj `
  --context MarketplaceAnalyticsDbContext `
  --output .\artifacts\MarketplaceAnalytics.sql
```

Review generated SQL before applying it. Verification scripts need not be committed.

## Health check

The API exposes `GET /health`. Application startup validates that a connection string
is present, but it does not connect to PostgreSQL. The endpoint performs the live
connectivity check and reports unhealthy without exposing credentials or exception
details. This deliberately distinguishes successful application startup from database
availability.

## Testing strategy

Normal tests do not require PostgreSQL. Integration tests verify registration, missing
and blank configuration failures, DbContext resolution, Npgsql provider selection,
and health-check registration. Live connectivity remains an explicit local verification
step when valid credentials are available.

## Security rules

- Never commit real database usernames or passwords.
- Never log a connection string.
- Prefer process-local environment variables or API User Secrets for development.
- Verify the target before applying any migration.
- Never automatically delete, recreate, drop, or reset a database.

## Troubleshooting

- **Connection refused:** confirm PostgreSQL is installed, running, and listening on
  the configured host and port.
- **Authentication failed:** verify the local role and secret outside source control.
- **Connection string missing:** set `ConnectionStrings__MarketplaceAnalyticsDatabase`.
- **Tool unavailable:** run `dotnet tool restore` from the repository root.
- **Factory cannot locate appsettings:** run commands from the repository root or the
  Infrastructure project directory.
- **Health endpoint is unhealthy while API runs:** startup configuration is valid, but
  the database is unavailable or credentials are invalid.

## Deferred work

Business entities, marketplace/eBay tables, raw ingestion, transformed data, reporting,
repositories, live operational database provisioning, background jobs, and deployment
automation remain deferred to their approved roadmap phases.
