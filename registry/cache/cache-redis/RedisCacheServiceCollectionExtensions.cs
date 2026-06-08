using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Cache;

/// <summary>DI wiring for the Redis adapter. Swap to another provider by changing this one call.</summary>
public static class RedisCacheServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisCacheOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<RedisCacheStore>();
        services.AddSingleton<ICacheStore>(sp => sp.GetRequiredService<RedisCacheStore>());
        services.AddSingleton<IRedisCacheStore>(sp => sp.GetRequiredService<RedisCacheStore>());

        return services;
    }
}
