using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.Conformance;

/// <summary>
/// Azure Blob Storage adapter run against a real endpoint (level B/C — Azurite emulator or a
/// sandbox account). Lives in the non-blocking nightly lane; skipped when no connection string
/// is set. Configure with <c>OUTLET_AZURE_TEST_CONNECTION_STRING</c> (e.g. <c>UseDevelopmentStorage=true</c>).
/// </summary>
[Trait("Category", "Live")]
public sealed class AzureBlobObjectStorageConformanceTests : ObjectStorageConformanceTests
{
    protected override Task<IObjectStorageHarness?> CreateOrSkipAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("OUTLET_AZURE_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            return Task.FromResult<IObjectStorageHarness?>(null);

        var container = "outlet-conformance-" + Guid.NewGuid().ToString("N")[..12];
        var storage = new AzureBlobObjectStorage(Options.Create(new AzureBlobObjectStorageOptions
        {
            ConnectionString = connectionString,
            ContainerName = container,
            CreateContainerIfNotExists = true,
        }));

        return Task.FromResult<IObjectStorageHarness?>(new Harness(storage, connectionString, container));
    }

    private sealed class Harness(AzureBlobObjectStorage storage, string connectionString, string container) : IObjectStorageHarness
    {
        public IObjectStorage Storage => storage;

        public async ValueTask DisposeAsync()
            => await new BlobContainerClient(connectionString, container).DeleteIfExistsAsync();
    }
}
