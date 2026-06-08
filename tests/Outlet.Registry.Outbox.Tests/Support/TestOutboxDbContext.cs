using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Outlet.Registry.Outbox;

namespace Outlet.Registry.Outbox.Tests.Support;

/// <summary>
/// A minimal application-style context that implements <see cref="IOutboxDbContext"/> and
/// applies the shipped <see cref="OutboxMessageConfiguration"/> — exactly what a consumer
/// would do to wire the EF Core adapter.
/// </summary>
public sealed class TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    // SQLite can't ORDER BY a DateTimeOffset; store it as sortable ISO-8601 TEXT. This is a
    // SQLite-emulator concern kept in the TEST context — real SQL Server / PostgreSQL order
    // DateTimeOffset natively, so the shipped adapter + configuration stay provider-neutral.
    private static readonly ValueConverter<DateTimeOffset, string> SqliteDateTimeOffset = new(
        value => value.ToString("O", CultureInfo.InvariantCulture),
        value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.Property(message => message.OccurredAt).HasConversion(SqliteDateTimeOffset);
            entity.Property(message => message.DispatchedAt).HasConversion(SqliteDateTimeOffset);
        });
    }
}
