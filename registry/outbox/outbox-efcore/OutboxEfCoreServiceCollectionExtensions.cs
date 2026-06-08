using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Outbox;

/// <summary>DI wiring for the EF Core outbox adapter. Swap storage by changing this one call.</summary>
public static class OutboxEfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OutboxEfCoreStore"/> as <see cref="IOutboxStore"/> (scoped, like
    /// the DbContext). You still wire your own context as <see cref="IOutboxDbContext"/>, e.g.
    /// <c>services.AddScoped&lt;IOutboxDbContext&gt;(sp =&gt; sp.GetRequiredService&lt;AppDbContext&gt;())</c>,
    /// and apply <see cref="OutboxMessageConfiguration"/> in <c>OnModelCreating</c>.
    /// </summary>
    public static IServiceCollection AddEfCoreOutbox(this IServiceCollection services)
    {
        services.AddScoped<IOutboxStore, OutboxEfCoreStore>();
        return services;
    }
}
