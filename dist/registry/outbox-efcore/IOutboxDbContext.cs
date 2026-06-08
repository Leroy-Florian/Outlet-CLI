using Microsoft.EntityFrameworkCore;

namespace Outlet.Registry.Outbox;

/// <summary>
/// Provider-specific companion to <see cref="IOutboxStore"/> (it lives BESIDE the
/// generic port, never inside it). Your application's <see cref="DbContext"/> implements
/// it, so the outbox append goes through the SAME change tracker / SaveChanges as your
/// business writes — making them atomic without leaking EF Core into the generic port.
/// </summary>
public interface IOutboxDbContext
{
    /// <summary>The set the adapter appends to and the relay reads from.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>Persists tracked changes — the relay uses it; the append side relies on
    /// your business <c>SaveChanges</c> instead, keeping the write atomic.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
