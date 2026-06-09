using Microsoft.Extensions.Options;
using Outlet.Registry.Cache;

namespace Outlet.Registry.Cache.Tests.Conformance;

/// <summary>
/// Redis adapter run against a REAL Redis server (level C). Tagged <c>Live</c> so the
/// hermetic PR lane skips it; the scheduled lane provides the server. Point it at a
/// non-default endpoint with the <c>OUTLET_REDIS</c> environment variable.
/// </summary>
[Trait("Category", "Live")]
public sealed class RedisCacheStoreConformanceTests : CacheStoreConformanceTests
{
    private static string Configuration =>
        Environment.GetEnvironmentVariable("OUTLET_REDIS") ?? "localhost:6379";

    protected override Task<ICacheStoreHarness> CreateHarnessAsync()
    {
        var store = new RedisCacheStore(Options.Create(new RedisCacheOptions
        {
            Configuration = Configuration,
            KeyPrefix = "outlet-test:",
        }));

        return Task.FromResult<ICacheStoreHarness>(new Harness(store));
    }

    private sealed class Harness(RedisCacheStore store) : ICacheStoreHarness
    {
        public ICacheStore Store => store;
        public ValueTask DisposeAsync() => store.DisposeAsync();
    }
}
