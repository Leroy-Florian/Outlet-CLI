using Microsoft.Extensions.Options;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.Conformance;

/// <summary>In-memory adapter run against its own dictionary (fully hermetic).</summary>
public sealed class InMemoryObjectStorageConformanceTests : ObjectStorageConformanceTests
{
    protected override Task<IBlobStorageHarness?> CreateOrSkipAsync()
    {
        var storage = new InMemoryObjectStorage(Options.Create(new InMemoryObjectStorageOptions()));
        return Task.FromResult<IBlobStorageHarness?>(new Harness(storage));
    }

    private sealed class Harness(IBlobStorage storage) : IBlobStorageHarness
    {
        public IBlobStorage Storage => storage;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
