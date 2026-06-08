using Dapper;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Outbox;

/// <summary>
/// Dapper adapter for <see cref="IOutboxStore"/>. Thin by design: it issues plain SQL on
/// the ambient <see cref="IOutboxDbSession"/> connection + transaction, so the append is
/// atomic with your business write. The SQL uses the ANSI/SQLite/PostgreSQL/MySQL
/// <c>LIMIT</c> form; on SQL Server swap <c>LIMIT</c> for <c>OFFSET … FETCH</c> in your
/// owned copy — that is the point of Outlet.
/// </summary>
public sealed class OutboxDapperStore(IOutboxDbSession session, IOptions<OutboxDapperOptions> options) : IOutboxStore
{
    private readonly OutboxDapperOptions _options = options.Value;

    public Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            INSERT INTO {_options.TableName}
                (Id, Type, Payload, Destination, Headers, OccurredAt, DispatchedAt)
            VALUES
                (@Id, @Type, @Payload, @Destination, @Headers, @OccurredAt, @DispatchedAt)
            """;

        return session.Connection.ExecuteAsync(new CommandDefinition(
            sql, message, session.Transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT Id, Type, Payload, Destination, Headers, OccurredAt, DispatchedAt
            FROM {_options.TableName}
            WHERE DispatchedAt IS NULL
            ORDER BY OccurredAt
            LIMIT @BatchSize
            """;

        var rows = await session.Connection.QueryAsync<OutboxMessage>(new CommandDefinition(
            sql, new { BatchSize = batchSize }, session.Transaction, cancellationToken: cancellationToken));

        return [.. rows];
    }

    public Task MarkDispatchedAsync(
        IReadOnlyList<Guid> messageIds,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            UPDATE {_options.TableName}
            SET DispatchedAt = @DispatchedAt
            WHERE Id IN @Ids
            """;

        return session.Connection.ExecuteAsync(new CommandDefinition(
            sql, new { DispatchedAt = dispatchedAt, Ids = messageIds }, session.Transaction, cancellationToken: cancellationToken));
    }
}
