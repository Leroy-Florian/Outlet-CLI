using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Outlet.Registry.Outbox;
using Outlet.Registry.Outbox.Tests.Support;

namespace Outlet.Registry.Outbox.Tests.Conformance;

/// <summary>EF Core adapter run against an in-memory SQLite database (real SQL, hermetic).</summary>
public sealed class OutboxEfCoreStoreConformanceTests : OutboxStoreConformanceTests
{
    protected override Task<IOutboxHarness> CreateHarnessAsync() => EfCoreOutboxHarness.CreateAsync();

    private sealed class EfCoreOutboxHarness(SqliteConnection connection, DbContextOptions<TestOutboxDbContext> options)
        : IOutboxHarness
    {
        public static async Task<IOutboxHarness> CreateAsync()
        {
            // One open connection keeps the in-memory database alive; every context reuses it.
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new TestOutboxDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new EfCoreOutboxHarness(connection, options);
        }

        public async Task RunUnitOfWorkAsync(Func<IOutboxStore, Task> work, bool commit)
        {
            await using var context = new TestOutboxDbContext(options);
            await using var transaction = await context.Database.BeginTransactionAsync();

            await work(new OutboxEfCoreStore(context));
            await context.SaveChangesAsync();

            if (commit)
                await transaction.CommitAsync();
            else
                await transaction.RollbackAsync();
        }

        public async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(int batchSize)
        {
            await using var context = new TestOutboxDbContext(options);
            return await new OutboxEfCoreStore(context).ReadPendingAsync(batchSize);
        }

        public async Task MarkDispatchedAsync(IReadOnlyList<Guid> messageIds, DateTimeOffset dispatchedAt)
        {
            await using var context = new TestOutboxDbContext(options);
            await new OutboxEfCoreStore(context).MarkDispatchedAsync(messageIds, dispatchedAt);
        }

        public async ValueTask DisposeAsync() => await connection.DisposeAsync();
    }
}
