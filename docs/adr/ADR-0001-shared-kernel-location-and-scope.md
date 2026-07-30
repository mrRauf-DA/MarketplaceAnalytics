# ADR-0001: Shared Kernel Location and Scope

## Status

Accepted

## Context

Future MarketplaceAnalytics domain models need consistent definitions for entity
identity, aggregate boundaries, value equality, and domain events. Duplicating
those mechanics in each feature would create incompatible behavior and make
business types depend on implementation details.

## Decision

Keep a minimal Shared Kernel in `MarketplaceAnalytics.Domain.Abstractions`.

The kernel contains only:

- `Entity<TId>`
- `AggregateRoot<TId>`
- `IDomainEvent`
- `ValueObject`

These types use only the .NET Base Class Library. No database, API, serialization,
dependency-injection, messaging, or vendor package is permitted in the kernel.

## Consequences

- Domain models share one tested equality and event-recording model.
- Inner-layer independence remains enforceable.
- Infrastructure can later implement persistence and event dispatch without those
  choices leaking into the domain.
- Changes to a shared primitive require careful compatibility review because they
  can affect every domain model.

## Rejected alternatives

### Separate SharedKernel project

Rejected for the current roadmap because the primitives are domain-only and a new
project would add a dependency boundary without an independent lifecycle.

### Put primitives in Application

Rejected because domain models would then need an outward dependency.

### Add Result, clock, user-context, persistence, or messaging abstractions now

Rejected as speculative and outside Phase 3. Such abstractions must be introduced
only by the roadmap phase that has a concrete requirement for them.

## Future compatibility

The chosen primitives do not assume PostgreSQL, eBay APIs, HTTP, background
workers, reporting technology, or deployment topology. Future layers may consume
the domain abstractions while dependencies continue to point inward.
