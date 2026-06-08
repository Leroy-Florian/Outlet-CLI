using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests;

public sealed class StorageDependencyInjectionTests
{
    [Fact]
    public void Should_ResolveInMemoryStorage_When_AddInMemoryObjectStorageIsCalled()
    {
        var services = new ServiceCollection();
        services.AddInMemoryObjectStorage();

        using var provider = services.BuildServiceProvider();

        provider.GetService<IObjectStorage>().Should().BeOfType<InMemoryObjectStorage>();
    }

    [Fact]
    public void Should_ResolveFileSystemStorage_When_AddFileSystemObjectStorageIsCalled()
    {
        var services = new ServiceCollection();
        services.AddFileSystemObjectStorage(o => o.RootPath = Path.GetTempPath());

        using var provider = services.BuildServiceProvider();

        provider.GetService<IObjectStorage>().Should().BeOfType<FileSystemObjectStorage>();
    }

    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddS3ObjectStorageIsCalled()
    {
        var services = new ServiceCollection();
        services.AddS3ObjectStorage(o =>
        {
            o.BucketName = "test-bucket";
            o.ServiceUrl = "http://localhost:9000";
            o.ForcePathStyle = true;
            o.AccessKeyId = "test";
            o.SecretAccessKey = "test";
        });

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<IObjectStorage>();
        var specific = provider.GetRequiredService<IS3ObjectStorage>();

        generic.Should().BeOfType<S3ObjectStorage>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddAzureBlobStorageIsCalled()
    {
        var services = new ServiceCollection();
        services.AddAzureBlobStorage(o =>
        {
            o.ConnectionString = "UseDevelopmentStorage=true";
            o.ContainerName = "test-container";
        });

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<IObjectStorage>();
        var specific = provider.GetRequiredService<IAzureBlobStorage>();

        generic.Should().BeOfType<AzureBlobObjectStorage>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        IObjectStorage Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider().GetRequiredService<IObjectStorage>();
        }

        Resolve(s => s.AddInMemoryObjectStorage()).Should().BeOfType<InMemoryObjectStorage>();
        Resolve(s => s.AddFileSystemObjectStorage(o => o.RootPath = Path.GetTempPath())).Should().BeOfType<FileSystemObjectStorage>();
        Resolve(s => s.AddS3ObjectStorage(o =>
        {
            o.BucketName = "b";
            o.ServiceUrl = "http://localhost:9000";
        })).Should().BeOfType<S3ObjectStorage>();
        Resolve(s => s.AddAzureBlobStorage(o =>
        {
            o.ConnectionString = "UseDevelopmentStorage=true";
            o.ContainerName = "c";
        })).Should().BeOfType<AzureBlobObjectStorage>();
    }
}
