using Dapper;
using Microsoft.Data.Sqlite;
using Outlet.Registry.Outbox;
using Outlet.Registry.Outbox.Tests.Support;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Outlet.Registry.Outbox.Tests.Conformance;

/// <summary>Dapper adapter run against an in-memory SQLite database (real SQL, hermetic).</summary>
public sealed class OutboxDapperStoreConformanceTests : OutboxStoreConformanceTests
{
    protected override Task<IOutboxHarness> CreateHarnessAsync() => DapperOutboxHarness.CreateAsync();

    private sealed class DapperOutboxHarness(SqliteConnection keepAlive, string connectionString) : IOutboxHarness
    {
        private const string CreateTableSql = """
            CREATE TABLE OutboxMessages (
                Id TEXT NOT NULL PRIMARY KEY,
                Type TEXT NOT NULL,
                Payload TEXT NOT NULL,
                Destination TEXT NULL,
                Headers TEXT NULL,
                OccurredAt TEXT NOT NULL,
                DispatchedAt TEXT NULL
            );
            """;

        private static readonly OutboxDapperOptions Options = new() { TableName = "OutboxMessages" };

        public static async Task<IOutboxHarness> CreateAsync()
        {
            SqliteDapperTypeHandlers.EnsureRegistered();

            // A private, shared-cache in-memory database; the kept-open connection keeps it alive
            // so relay reads on separate connections see committed state.
            var connectionString = $"Data Source=outbox_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var keepAlive = new SqliteConnection(connectionString);
            await keepAlive.OpenAsync();
            await keepAlive.ExecuteAsync(CreateTableSql);

            return new DapperOutboxHarness(keepAlive, connectionString);
        }

        public async Task RunUnitOfWorkAsync(Func<IOutboxStore, Task> work, bool commit)
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var store = new OutboxDapperStore(new AmbientOutboxDbSession(connection, transaction), MicrosoftOptions.Create(Options));
            await work(store);

            if (commit)
                await transaction.CommitAsync();
            else
                await transaction.RollbackAsync();
        }

        public async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize)
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            var store = new OutboxDapperStore(new AmbientOutboxDbSession(connection, null), MicrosoftOptions.Create(Options));
            return await store.ReadPendingAsync(batchSize);
        }

        public async Task MarkDispatchedAsync(IReadOnlyList<Guid> messageIds, DateTimeOffset dispatchedAt)
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var store = new OutboxDapperStore(new AmbientOutboxDbSession(connection, transaction), MicrosoftOptions.Create(Options));
            await store.MarkDispatchedAsync(messageIds, dispatchedAt);

            await transaction.CommitAsync();
        }

        public async ValueTask DisposeAsync() => await keepAlive.DisposeAsync();
    }
}
