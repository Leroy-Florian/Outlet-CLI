using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the AWS S3 adapter. Swap to another provider by changing this one call.</summary>
public static class S3ObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddS3ObjectStorage(
        this IServiceCollection services,
        Action<S3ObjectStorageOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<S3ObjectStorage>();
        services.AddSingleton<IBlobStorage>(sp => sp.GetRequiredService<S3ObjectStorage>());
        services.AddSingleton<IS3ObjectStorage>(sp => sp.GetRequiredService<S3ObjectStorage>());

        return services;
    }
}
