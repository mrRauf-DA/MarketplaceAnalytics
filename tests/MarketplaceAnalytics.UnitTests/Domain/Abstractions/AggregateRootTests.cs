using MarketplaceAnalytics.Domain.Abstractions;
using Xunit;

namespace MarketplaceAnalytics.UnitTests.Domain.Abstractions;

public sealed class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_RecordsEventInOrder()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var first = new TestEvent("first");
        var second = new TestEvent("second");

        aggregate.Record(first);
        aggregate.Record(second);

        Assert.Equal([first, second], aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllRecordedEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.Record(new TestEvent("event"));

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    private sealed record TestEvent(string Name) : IDomainEvent;

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Record(IDomainEvent domainEvent)
        {
            RaiseDomainEvent(domainEvent);
        }
    }
}
