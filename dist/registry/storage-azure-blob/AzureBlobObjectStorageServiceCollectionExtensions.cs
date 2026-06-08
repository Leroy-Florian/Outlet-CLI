using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the Azure Blob Storage adapter. Swap to another provider by changing this one call.</summary>
public static class AzureBlobObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        Action<AzureBlobObjectStorageOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<AzureBlobObjectStorage>();
        services.AddSingleton<IObjectStorage>(sp => sp.GetRequiredService<AzureBlobObjectStorage>());
        services.AddSingleton<IAzureBlobStorage>(sp => sp.GetRequiredService<AzureBlobObjectStorage>());

        return services;
    }
}
