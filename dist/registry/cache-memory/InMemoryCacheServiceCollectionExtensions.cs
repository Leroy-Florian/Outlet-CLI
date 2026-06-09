using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Cache;

/// <summary>DI wiring for the in-memory adapter. Swap to another provider by changing this one call.</summary>
public static class InMemoryCacheServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryCache(this IServiceCollection services, Action<InMemoryCacheOptions>? configure = null)
    {
        var options = new InMemoryCacheOptions();
        configure?.Invoke(options);

        services.AddMemoryCache(memory =>
        {
            if (options.SizeLimit is { } limit)
                memory.SizeLimit = limit;
        });

        services.AddSingleton<ICacheStore, InMemoryCacheStore>();

        return services;
    }
}
