using Outlet.Registry.Outbox;

namespace Outlet.Registry.Outbox.Tests;

public sealed class OutboxContractTests
{
    [Fact]
    public void Should_StampFreshIdAndPendingState_When_Created()
    {
        var occurredAt = new DateTimeOffset(2026, 6, 8, 10, 0, 0, TimeSpan.Zero);

        var message = OutboxMessage.Create("order.placed", """{"id":1}""", occurredAt);

        message.Id.Should().NotBe(Guid.Empty);
        message.Type.Should().Be("order.placed");
        message.Payload.Should().Be("""{"id":1}""");
        message.OccurredAt.Should().Be(occurredAt);
        message.DispatchedAt.Should().BeNull();
    }

    [Fact]
    public void Should_GiveEachMessageADistinctId()
    {
        var first = OutboxMessage.Create("t", "p", DateTimeOffset.UtcNow);
        var second = OutboxMessage.Create("t", "p", DateTimeOffset.UtcNow);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Should_DefaultRoutingMetadataToNull_When_Omitted()
    {
        var message = OutboxMessage.Create("t", "p", DateTimeOffset.UtcNow);

        message.Destination.Should().BeNull();
        message.Headers.Should().BeNull();
    }

    [Fact]
    public void Should_CarryRoutingMetadata_When_Provided()
    {
        var message = OutboxMessage.Create("t", "p", DateTimeOffset.UtcNow, destination: "orders", headers: """{"k":"v"}""");

        message.Destination.Should().Be("orders");
        message.Headers.Should().Be("""{"k":"v"}""");
    }
}
