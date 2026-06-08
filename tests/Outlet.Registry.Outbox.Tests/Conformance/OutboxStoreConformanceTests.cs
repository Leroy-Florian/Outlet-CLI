using Outlet.Registry.Outbox;

namespace Outlet.Registry.Outbox.Tests.Conformance;

/// <summary>
/// Drives an <see cref="IOutboxStore"/> against its real storage backend. Appends run inside
/// a unit of work the harness commits or rolls back; reads/marks run on the relay side
/// (separate transaction) — so the suite can assert the outbox's defining property:
/// the append is atomic with the business transaction.
/// </summary>
public interface IOutboxHarness : IAsyncDisposable
{
    /// <summary>Runs <paramref name="work"/> against a store enrolled in a fresh unit of work,
    /// then commits or rolls it back atomically.</summary>
    Task RunUnitOfWorkAsync(Func<IOutboxStore, Task> work, bool commit);

    /// <summary>Relay-side read on a fresh connection — sees only committed state.</summary>
    Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize);

    /// <summary>Relay-side mark on its own transaction.</summary>
    Task MarkDispatchedAsync(IReadOnlyList<Guid> messageIds, DateTimeOffset dispatchedAt);
}

/// <summary>
/// Reusable PORT conformance suite — "every IOutboxStore must behave this way". Each adapter
/// runs it against its own real backend (hermetic SQLite), so swappability is TESTED, not
/// asserted.
/// </summary>
public abstract class OutboxStoreConformanceTests
{
    protected abstract Task<IOutboxHarness> CreateHarnessAsync();

    [Fact]
    public async Task Should_PersistPendingMessage_When_UnitOfWorkCommits()
    {
        await using var harness = await CreateHarnessAsync();
        var message = OutboxMessage.Create("order.placed", """{"id":1}""", DateTimeOffset.UtcNow);

        await harness.RunUnitOfWorkAsync(store => store.AppendAsync(message), commit: true);

        var pending = await harness.ReadPendingAsync(10);
        pending.Should().ContainSingle(m => m.Id == message.Id);
    }

    [Fact]
    public async Task Should_NotPersistMessage_When_UnitOfWorkRollsBack()
    {
        await using var harness = await CreateHarnessAsync();
        var message = OutboxMessage.Create("order.placed", """{"id":1}""", DateTimeOffset.UtcNow);

        await harness.RunUnitOfWorkAsync(store => store.AppendAsync(message), commit: false);

        var pending = await harness.ReadPendingAsync(10);
        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RoundTripPayloadAndMetadata_When_Read()
    {
        await using var harness = await CreateHarnessAsync();
        var occurredAt = new DateTimeOffset(2026, 6, 8, 10, 0, 0, TimeSpan.Zero);
        var message = OutboxMessage.Create("order.placed", """{"id":1}""", occurredAt, destination: "orders", headers: """{"k":"v"}""");

        await harness.RunUnitOfWorkAsync(store => store.AppendAsync(message), commit: true);

        var stored = (await harness.ReadPendingAsync(10)).Single();
        stored.Type.Should().Be("order.placed");
        stored.Payload.Should().Be("""{"id":1}""");
        stored.Destination.Should().Be("orders");
        stored.Headers.Should().Be("""{"k":"v"}""");
        stored.OccurredAt.Should().Be(occurredAt);
        stored.DispatchedAt.Should().BeNull();
    }

    [Fact]
    public async Task Should_ExcludeDispatchedMessages_From_Pending()
    {
        await using var harness = await CreateHarnessAsync();
        var message = OutboxMessage.Create("t", "{}", DateTimeOffset.UtcNow);
        await harness.RunUnitOfWorkAsync(store => store.AppendAsync(message), commit: true);

        await harness.MarkDispatchedAsync([message.Id], DateTimeOffset.UtcNow);

        (await harness.ReadPendingAsync(10)).Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnOldestFirst_And_HonorBatchSize()
    {
        await using var harness = await CreateHarnessAsync();
        var baseTime = new DateTimeOffset(2026, 6, 8, 10, 0, 0, TimeSpan.Zero);
        var first = OutboxMessage.Create("t", "1", baseTime);
        var second = OutboxMessage.Create("t", "2", baseTime.AddSeconds(1));
        var third = OutboxMessage.Create("t", "3", baseTime.AddSeconds(2));

        await harness.RunUnitOfWorkAsync(async store =>
        {
            await store.AppendAsync(third);
            await store.AppendAsync(first);
            await store.AppendAsync(second);
        }, commit: true);

        var pending = await harness.ReadPendingAsync(2);

        pending.Select(m => m.Payload).Should().Equal("1", "2");
    }
}
