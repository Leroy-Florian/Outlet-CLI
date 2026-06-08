using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the in-memory adapter. Swap to another provider by changing this one call.</summary>
public static class InMemoryObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryObjectStorage(
        this IServiceCollection services,
        Action<InMemoryObjectStorageOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        return services;
    }
}
