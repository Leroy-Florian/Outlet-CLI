using Microsoft.Extensions.Options;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.Conformance;

/// <summary>In-memory adapter run against its own dictionary (fully hermetic).</summary>
public sealed class InMemoryObjectStorageConformanceTests : ObjectStorageConformanceTests
{
    protected override Task<IObjectStorageHarness?> CreateOrSkipAsync()
    {
        var storage = new InMemoryObjectStorage(Options.Create(new InMemoryObjectStorageOptions()));
        return Task.FromResult<IObjectStorageHarness?>(new Harness(storage));
    }

    private sealed class Harness(IObjectStorage storage) : IObjectStorageHarness
    {
        public IObjectStorage Storage => storage;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
