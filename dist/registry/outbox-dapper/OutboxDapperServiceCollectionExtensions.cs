using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Outbox;

/// <summary>DI wiring for the Dapper outbox adapter. Swap storage by changing this one call.</summary>
public static class OutboxDapperServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OutboxDapperStore"/> as <see cref="IOutboxStore"/> (scoped). You
    /// still wire <see cref="IOutboxDbSession"/> yourself (scoped, bound to your unit of work),
    /// since the connection/transaction lifecycle is owned by your business code.
    /// </summary>
    public static IServiceCollection AddDapperOutbox(this IServiceCollection services, Action<OutboxDapperOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddScoped<IOutboxStore, OutboxDapperStore>();
        return services;
    }
}
