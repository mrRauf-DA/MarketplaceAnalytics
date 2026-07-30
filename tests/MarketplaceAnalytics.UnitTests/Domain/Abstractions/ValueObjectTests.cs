using MarketplaceAnalytics.Domain.Abstractions;
using Xunit;

namespace MarketplaceAnalytics.UnitTests.Domain.Abstractions;

public sealed class ValueObjectTests
{
    [Fact]
    public void ValueObjects_WithSameComponents_AreEqual()
    {
        var first = new TestValue("ABC", [1, 2, 3]);
        var second = new TestValue("ABC", [1, 2, 3]);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ValueObjects_WithDifferentComponents_AreNotEqual()
    {
        var first = new TestValue("ABC", [1, 2, 3]);
        var second = new TestValue("ABC", [1, 2, 4]);

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    [Fact]
    public void ValueObjects_WithSameComponentsButDifferentRuntimeTypes_AreNotEqual()
    {
        ValueObject first = new TestValue("ABC", [1, 2, 3]);
        ValueObject second = new OtherTestValue("ABC", [1, 2, 3]);

        Assert.NotEqual(first, second);
    }

    private sealed class TestValue(string code, IReadOnlyCollection<int> numbers) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return code;
            yield return numbers;
        }
    }

    private sealed class OtherTestValue(string code, IReadOnlyCollection<int> numbers) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return code;
            yield return numbers;
        }
    }
}
