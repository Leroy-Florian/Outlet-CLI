using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests;

public sealed class StorageDependencyInjectionTests
{
    [Fact]
    public void Should_ResolveInMemoryStorage_When_AddInMemoryBlobStorageIsCalled()
    {
        var services = new ServiceCollection();
        services.AddInMemoryBlobStorage();

        using var provider = services.BuildServiceProvider();

        provider.GetService<IBlobStorage>().Should().BeOfType<InMemoryBlobStorage>();
    }

    [Fact]
    public void Should_ResolveFileSystemStorage_When_AddFileSystemBlobStorageIsCalled()
    {
        var services = new ServiceCollection();
        services.AddFileSystemBlobStorage(o => o.RootPath = Path.GetTempPath());

        using var provider = services.BuildServiceProvider();

        provider.GetService<IBlobStorage>().Should().BeOfType<FileSystemBlobStorage>();
    }

    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddS3BlobStorageIsCalled()
    {
        var services = new ServiceCollection();
        services.AddS3BlobStorage(o =>
        {
            o.BucketName = "test-bucket";
            o.ServiceUrl = "http://localhost:9000";
            o.ForcePathStyle = true;
            o.AccessKeyId = "test";
            o.SecretAccessKey = "test";
        });

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<IBlobStorage>();
        var specific = provider.GetRequiredService<IS3BlobStorage>();

        generic.Should().BeOfType<S3BlobStorage>();
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
        var generic = provider.GetRequiredService<IBlobStorage>();
        var specific = provider.GetRequiredService<IAzureBlobStorage>();

        generic.Should().BeOfType<AzureBlobStorage>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        IBlobStorage Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider().GetRequiredService<IBlobStorage>();
        }

        Resolve(s => s.AddInMemoryBlobStorage()).Should().BeOfType<InMemoryBlobStorage>();
        Resolve(s => s.AddFileSystemBlobStorage(o => o.RootPath = Path.GetTempPath())).Should().BeOfType<FileSystemBlobStorage>();
        Resolve(s => s.AddS3BlobStorage(o =>
        {
            o.BucketName = "b";
            o.ServiceUrl = "http://localhost:9000";
        })).Should().BeOfType<S3BlobStorage>();
        Resolve(s => s.AddAzureBlobStorage(o =>
        {
            o.ConnectionString = "UseDevelopmentStorage=true";
            o.ContainerName = "c";
        })).Should().BeOfType<AzureBlobStorage>();
    }
}
