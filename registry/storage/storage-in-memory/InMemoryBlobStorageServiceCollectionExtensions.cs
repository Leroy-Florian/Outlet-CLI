using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the in-memory adapter. Swap to another provider by changing this one call.</summary>
public static class InMemoryBlobStorageServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryBlobStorage(
        this IServiceCollection services,
        Action<InMemoryBlobStorageOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddSingleton<IBlobStorage, InMemoryBlobStorage>();
        return services;
    }
}
