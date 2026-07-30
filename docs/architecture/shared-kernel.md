# Shared Kernel

## Purpose

The MarketplaceAnalytics Shared Kernel is the smallest set of framework-independent
domain primitives that future business models can reuse consistently. It lives in
`MarketplaceAnalytics.Domain` because every primitive represents a domain concern
and must remain independent of Application, Infrastructure, API, persistence, and
external integrations.

## Included primitives

| Primitive | Responsibility |
| --- | --- |
| `Entity<TId>` | Defines identity-based equality for domain entities. |
| `AggregateRoot<TId>` | Defines an aggregate boundary and records domain events raised by that aggregate. |
| `IDomainEvent` | Marks a fact that occurred inside the domain. |
| `ValueObject` | Defines structural equality from immutable component values. |

## Usage rules

- Put shared primitives under `MarketplaceAnalytics.Domain.Abstractions`.
- Derive business entities from `Entity<TId>` only when identity, rather than all
  properties, defines equality.
- Derive only aggregate entry points from `AggregateRoot<TId>`.
- Implement `IDomainEvent` with immutable event types.
- Derive immutable concepts from `ValueObject` and return every equality-relevant
  value from `GetEqualityComponents()`.
- Keep database annotations, serialization behavior, transport contracts, logging,
  clocks, and external-service concerns outside the Shared Kernel.
- Do not add a primitive until at least two domain concepts require the same
  behavior or the primitive enforces an approved architecture rule.

## Domain-event lifecycle

An aggregate records events in the order in which they occur. The Application or
Infrastructure layer may read `DomainEvents` after a successful operation and
dispatch them using a future approved implementation. After successful dispatch,
the caller clears the recorded events with `ClearDomainEvents()`.

This phase does not define dispatchers, handlers, persistence behavior, retries, or
transaction semantics.

## Equality guarantees

- Entities compare equal only when their runtime type and identifier are equal.
- Value objects compare equal only when their runtime type and ordered equality
  components are equal.
- Nested enumerable value-object components are compared structurally.
- Equal objects return equal hash codes.

## Verification

Run:

```powershell
dotnet test .\MarketplaceAnalytics.sln
```

Unit tests protect identity equality, value equality, nested collection equality,
domain-event order, and event clearing. Existing architecture tests protect the
Shared Kernel from outward dependencies.
