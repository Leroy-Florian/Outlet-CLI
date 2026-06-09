using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Outlet.Registry.Cache;

/// <summary>DI wiring for the Memcached adapter. Swap to another provider by changing this one call.</summary>
public static class MemcachedCacheServiceCollectionExtensions
{
    public static IServiceCollection AddMemcachedCache(this IServiceCollection services, Action<MemcachedCacheOptions> configure)
    {
        var options = new MemcachedCacheOptions();
        configure(options);

        services.Configure(configure);

        // Enyim's client resolves an ILoggerFactory; fall back to a no-op one when the host
        // has not configured logging, so AddMemcachedCache stays a true drop-in.
        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddEnyimMemcached(memcached => memcached.AddServer(options.Host, options.Port));

        services.AddSingleton<ICacheStore, MemcachedCacheStore>();

        return services;
    }
}
