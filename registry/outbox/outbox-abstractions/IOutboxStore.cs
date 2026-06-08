namespace Outlet.Registry.Outbox;

/// <summary>
/// Generic transactional-outbox port — identical across every storage adapter so
/// providers stay swappable. No provider specifics ever leak into this interface;
/// HOW a message enrolls in the ambient transaction is an adapter concern (a
/// companion type beside this port), never part of the generic contract.
///
/// The split mirrors the pattern: <see cref="AppendAsync"/> runs INSIDE the caller's
/// business unit of work (atomic with the state change), while
/// <see cref="ReadPendingAsync"/> / <see cref="MarkDispatchedAsync"/> are the relay
/// side, run later on their own transaction. The relay itself is a separate concern
/// composed over this port, never embedded here.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Persists <paramref name="message"/> as part of the caller's ongoing transaction /
    /// unit of work. It does NOT commit — the surrounding business commit makes the
    /// message and the state change atomic (that is the whole point of the outbox).
    /// </summary>
    Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Relay side: fetches up to <paramref name="batchSize"/> not-yet-dispatched
    /// messages, oldest first.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Relay side: marks the given messages dispatched once published, stamping
    /// <paramref name="dispatchedAt"/> (supplied by the caller — inject your clock).
    /// </summary>
    Task MarkDispatchedAsync(
        IReadOnlyList<Guid> messageIds,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default);
}
