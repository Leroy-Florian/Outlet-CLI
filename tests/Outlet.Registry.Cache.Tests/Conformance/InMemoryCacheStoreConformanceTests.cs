using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Outlet.Registry.Cache;

namespace Outlet.Registry.Cache.Tests.Conformance;

/// <summary>In-memory adapter run against a real <see cref="IMemoryCache"/> — fully hermetic, no server.</summary>
public sealed class InMemoryCacheStoreConformanceTests : CacheStoreConformanceTests
{
    protected override Task<ICacheStoreHarness> CreateHarnessAsync()
    {
        var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var store = new InMemoryCacheStore(cache);

        return Task.FromResult<ICacheStoreHarness>(new Harness(cache, store));
    }

    private sealed class Harness(MemoryCache cache, ICacheStore store) : ICacheStoreHarness
    {
        public ICacheStore Store => store;

        public ValueTask DisposeAsync()
        {
            cache.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
