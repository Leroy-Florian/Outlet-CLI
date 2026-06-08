using System.Data.Common;

namespace Outlet.Registry.Outbox;

/// <summary>
/// Provider-specific companion to <see cref="IOutboxStore"/> (it lives BESIDE the
/// generic port, never inside it). It exposes the ambient SQL connection and the
/// optional transaction your business write is using, so the outbox append runs on
/// the SAME connection + transaction — making them atomic.
///
/// You own the connection/transaction lifecycle (that is the whole point of the
/// outbox pattern): register this scoped and bind it to your unit of work. On the
/// relay side, point it at a fresh connection (with or without its own transaction).
/// </summary>
public interface IOutboxDbSession
{
    /// <summary>The open connection the append/read runs on.</summary>
    DbConnection Connection { get; }

    /// <summary>The ambient transaction to enlist in, or <c>null</c> for auto-commit.</summary>
    DbTransaction? Transaction { get; }
}
