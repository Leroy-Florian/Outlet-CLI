using Microsoft.EntityFrameworkCore;

namespace Outlet.Registry.Outbox;

/// <summary>
/// EF Core adapter for <see cref="IOutboxStore"/>. Thin by design: the append only
/// tracks the entity (no commit) so your business <c>SaveChanges</c> persists the
/// message and the state change in one transaction; the relay methods own their own
/// <c>SaveChanges</c>.
/// </summary>
public sealed class OutboxEfCoreStore(IOutboxDbContext dbContext) : IOutboxStore
{
    public Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        // Enrolls in the caller's unit of work; the business SaveChanges commits it atomically.
        dbContext.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken cancellationToken = default) =>
        [.. await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.DispatchedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)];

    public async Task MarkDispatchedAsync(
        IReadOnlyList<Guid> messageIds,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.OutboxMessages
            .Where(message => messageIds.Contains(message.Id))
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
            message.DispatchedAt = dispatchedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
