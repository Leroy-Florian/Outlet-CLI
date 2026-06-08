using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the Azure Blob Storage adapter. Swap to another provider by changing this one call.</summary>
public static class AzureBlobStorageServiceCollectionExtensions
{
    public static IServiceCollection AddAzureBlobStorage(
        this IServiceCollection services,
        Action<AzureBlobStorageOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<AzureBlobStorage>();
        services.AddSingleton<IBlobStorage>(sp => sp.GetRequiredService<AzureBlobStorage>());
        services.AddSingleton<IAzureBlobStorage>(sp => sp.GetRequiredService<AzureBlobStorage>());

        return services;
    }
}
