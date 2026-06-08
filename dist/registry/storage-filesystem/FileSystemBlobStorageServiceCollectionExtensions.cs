using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the filesystem adapter. Swap to another provider by changing this one call.</summary>
public static class FileSystemBlobStorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemBlobStorage(
        this IServiceCollection services,
        Action<FileSystemBlobStorageOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IBlobStorage, FileSystemBlobStorage>();
        return services;
    }
}
