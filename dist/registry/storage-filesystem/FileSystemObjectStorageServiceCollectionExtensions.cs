using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Storage;

/// <summary>DI wiring for the filesystem adapter. Swap to another provider by changing this one call.</summary>
public static class FileSystemObjectStorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemObjectStorage(
        this IServiceCollection services,
        Action<FileSystemObjectStorageOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IObjectStorage, FileSystemObjectStorage>();
        return services;
    }
}
