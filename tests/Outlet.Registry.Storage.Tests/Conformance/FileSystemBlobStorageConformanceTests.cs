using Microsoft.Extensions.Options;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.Conformance;

/// <summary>Filesystem adapter run against a throwaway temp directory (real I/O, hermetic).</summary>
public sealed class FileSystemBlobStorageConformanceTests : BlobStorageConformanceTests
{
    protected override Task<IBlobStorageHarness?> CreateOrSkipAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "outlet-storage-conformance", Guid.NewGuid().ToString("N"));
        var storage = new FileSystemBlobStorage(Options.Create(new FileSystemBlobStorageOptions { RootPath = root }));
        return Task.FromResult<IBlobStorageHarness?>(new Harness(storage, root));
    }

    private sealed class Harness(IBlobStorage storage, string root) : IBlobStorageHarness
    {
        public IBlobStorage Storage => storage;

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
            return ValueTask.CompletedTask;
        }
    }
}
