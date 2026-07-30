using MarketplaceAnalytics.Domain.Abstractions;
using Xunit;

namespace MarketplaceAnalytics.UnitTests.Domain.Abstractions;

public sealed class EntityTests
{
    [Fact]
    public void Entities_WithSameTypeAndId_AreEqual()
    {
        var first = new TestEntity(42);
        var second = new TestEntity(42);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentIds_AreNotEqual()
    {
        var first = new TestEntity(42);
        var second = new TestEntity(7);

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void Entities_WithSameIdButDifferentRuntimeTypes_AreNotEqual()
    {
        Entity<int> first = new TestEntity(42);
        Entity<int> second = new OtherTestEntity(42);

        Assert.NotEqual(first, second);
    }

    private sealed class TestEntity(int id) : Entity<int>(id);

    private sealed class OtherTestEntity(int id) : Entity<int>(id);
}
