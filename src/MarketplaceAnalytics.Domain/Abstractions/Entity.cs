namespace MarketplaceAnalytics.Domain.Abstractions;

/// <summary>
/// Base type for domain objects whose identity remains stable over their lifetime.
/// </summary>
/// <typeparam name="TId">The entity identifier type.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public TId Id { get; }

    public bool Equals(Entity<TId>? other)
    {
        return other is not null
            && GetType() == other.GetType()
            && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
