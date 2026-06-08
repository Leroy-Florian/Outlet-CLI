using System.Data.Common;
using Outlet.Registry.Outbox;

namespace Outlet.Registry.Outbox.Tests.Support;

/// <summary>Test double for <see cref="IOutboxDbSession"/>: a connection + optional transaction.</summary>
public sealed record AmbientOutboxDbSession(DbConnection Connection, DbTransaction? Transaction) : IOutboxDbSession;
