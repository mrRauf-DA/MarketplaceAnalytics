using System.Collections;

namespace MarketplaceAnalytics.Domain.Abstractions;

/// <summary>
/// Base type for immutable domain concepts defined by their component values.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        return other is not null
            && GetType() == other.GetType()
            && GetEqualityComponents().SequenceEqual(
                other.GetEqualityComponents(),
                EqualityComponentComparer.Instance);
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GetType());

        foreach (var component in GetEqualityComponents())
        {
            AddComponentHash(ref hash, component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }

    private static void AddComponentHash(ref HashCode hash, object? component)
    {
        if (component is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                AddComponentHash(ref hash, item);
            }

            return;
        }

        hash.Add(component);
    }

    private sealed class EqualityComponentComparer : IEqualityComparer<object?>
    {
        public static EqualityComponentComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x is IEnumerable left and not string
                && y is IEnumerable right and not string)
            {
                return left.Cast<object?>().SequenceEqual(
                    right.Cast<object?>(),
                    Instance);
            }

            return x.Equals(y);
        }

        public int GetHashCode(object? obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}
