# Configuration Foundation

## Purpose

Prompt 004 establishes strongly typed, startup-validated host configuration for
MarketplaceAnalytics while keeping configuration frameworks outside Domain,
Application, and the Shared Kernel.

## Ownership

The configuration boundary belongs to `MarketplaceAnalytics.API`, the executable
host and composition root:

- `MarketplaceAnalyticsOptions` owns the `MarketplaceAnalytics` section name and
  the strongly typed values.
- `MarketplaceAnalyticsConfigurationExtensions` binds and validates the section.
- `Program.cs` registers the options before building the host.

Domain and Application code must not read configuration files, environment
variables, `IConfiguration`, or `IOptions`. Infrastructure-specific options may be
added to Infrastructure only when an approved phase introduces a concrete
Infrastructure component.

## Base configuration

The tracked `appsettings.json` contains safe, non-secret defaults:

```json
{
  "MarketplaceAnalytics": {
    "ApplicationName": "MarketplaceAnalytics",
    "DataDirectory": "data"
  }
}
```

`ApplicationName` and `DataDirectory` are required and must not be blank.
`DataDirectory` may remain relative; validation does not create or require the
directory.

`appsettings.Development.json` is intentionally minimal. Development-specific,
non-secret overrides may be added there when they are safe to share.

## Environment overrides

ASP.NET Core's standard configuration pipeline loads environment variables after
JSON files, so environment values override tracked defaults. Use a double
underscore for hierarchy:

```text
MarketplaceAnalytics__ApplicationName
MarketplaceAnalytics__DataDirectory
```

No custom environment-variable parser is used.

## Secret safety

Tracked appsettings files must contain only safe shared values. Never commit
passwords, tokens, client secrets, refresh tokens, connection strings, usernames,
or private endpoints.

The repository ignores `.env`, `.env.*`, `appsettings.Local.json`, and
`secrets.json`; `.env.example` remains eligible for tracking when a future phase
has a concrete need for a safe template. The application does not require or parse
`.env` files.

## Startup validation

Configuration registration uses the standard .NET Options pattern with
`ValidateOnStart()`. Host startup fails with an `OptionsValidationException` when
either required value is missing or blank. Validation has no filesystem side
effects.

## Verification

Run the focused configuration tests:

```powershell
dotnet test .\tests\MarketplaceAnalytics.IntegrationTests\MarketplaceAnalytics.IntegrationTests.csproj --configuration Release
```

Run all architecture and solution tests:

```powershell
dotnet test .\MarketplaceAnalytics.sln --configuration Release
```

Prompt 004 does not implement PostgreSQL, database connectivity, connection
strings, eBay configuration, credentials, authentication, or API integration.
The next roadmap phase is Phase 5 — PostgreSQL.
