using Microsoft.Extensions.Options;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.Conformance;

/// <summary>Filesystem adapter run against a throwaway temp directory (real I/O, hermetic).</summary>
public sealed class FileSystemObjectStorageConformanceTests : ObjectStorageConformanceTests
{
    protected override Task<IObjectStorageHarness?> CreateOrSkipAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "outlet-storage-conformance", Guid.NewGuid().ToString("N"));
        var storage = new FileSystemObjectStorage(Options.Create(new FileSystemObjectStorageOptions { RootPath = root }));
        return Task.FromResult<IObjectStorageHarness?>(new Harness(storage, root));
    }

    private sealed class Harness(IObjectStorage storage, string root) : IObjectStorageHarness
    {
        public IObjectStorage Storage => storage;

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
            return ValueTask.CompletedTask;
        }
    }
}
