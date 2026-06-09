using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the AWS S3 adapter. Swap to another provider by changing this one call.</summary>
public static class S3BlobStorageServiceCollectionExtensions
{
    public static IServiceCollection AddS3BlobStorage(
        this IServiceCollection services,
        Action<S3BlobStorageOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<S3BlobStorage>();
        services.AddSingleton<IBlobStorage>(sp => sp.GetRequiredService<S3BlobStorage>());
        services.AddSingleton<IS3BlobStorage>(sp => sp.GetRequiredService<S3BlobStorage>());

        return services;
    }
}
