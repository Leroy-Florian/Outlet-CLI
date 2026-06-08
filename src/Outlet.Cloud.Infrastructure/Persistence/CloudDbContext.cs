using Microsoft.EntityFrameworkCore;

namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>EF Core context for the Cloud bounded context (organizations + memberships).</summary>
public sealed class CloudDbContext(DbContextOptions<CloudDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationRecord> Organizations => Set<OrganizationRecord>();
    public DbSet<MembershipRecord> OrganizationMembers => Set<MembershipRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationRecord>(builder =>
        {
            builder.ToTable("organizations");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Slug).IsRequired();
            builder.HasIndex(o => o.Slug).IsUnique();
            builder.Property(o => o.Name).IsRequired();

            builder.HasMany(o => o.Members)
                .WithOne()
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MembershipRecord>(builder =>
        {
            builder.ToTable("organization_members");
            builder.HasKey(m => new { m.OrganizationId, m.UserId });
            builder.Property(m => m.Role).HasConversion<string>().IsRequired();
        });
    }
}
