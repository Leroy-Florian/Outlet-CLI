using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Outlet.Registry.Outbox;

/// <summary>
/// EF Core mapping for <see cref="OutboxMessage"/>. Apply it from your context's
/// <c>OnModelCreating</c> (<c>modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration())</c>).
/// The composite index serves the relay's "oldest undispatched first" scan.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.OccurredAt).IsRequired();

        builder.HasIndex(message => new { message.DispatchedAt, message.OccurredAt });
    }
}
