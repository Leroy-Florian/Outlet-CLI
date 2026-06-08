using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Cache;

namespace Outlet.Registry.Cache.Tests.Conformance;

/// <summary>
/// Memcached adapter run against a REAL Memcached server (level C). Tagged <c>Live</c> so
/// the hermetic PR lane skips it; the scheduled lane provides the server. Point it at a
/// non-default endpoint with the <c>OUTLET_MEMCACHED</c> environment variable
/// (format <c>host:port</c>).
/// </summary>
[Trait("Category", "Live")]
public sealed class MemcachedCacheStoreConformanceTests : CacheStoreConformanceTests
{
    protected override Task<ICacheStoreHarness> CreateHarnessAsync()
    {
        var (host, port) = Endpoint();

        var services = new ServiceCollection();
        services.AddMemcachedCache(o =>
        {
            o.Host = host;
            o.Port = port;
            o.KeyPrefix = "outlet-test:";
        });

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ICacheStore>();

        return Task.FromResult<ICacheStoreHarness>(new Harness(provider, store));
    }

    private static (string Host, int Port) Endpoint()
    {
        var raw = Environment.GetEnvironmentVariable("OUTLET_MEMCACHED") ?? "localhost:11211";
        var parts = raw.Split(':', 2);
        var port = parts.Length == 2 && int.TryParse(parts[1], out var parsed) ? parsed : 11211;
        return (parts[0], port);
    }

    private sealed class Harness(ServiceProvider provider, ICacheStore store) : ICacheStoreHarness
    {
        public ICacheStore Store => store;
        public ValueTask DisposeAsync() => provider.DisposeAsync();
    }
}
