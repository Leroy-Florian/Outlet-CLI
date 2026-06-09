using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Outlet.Registry.Cache;

namespace Outlet.Registry.Cache.Tests.ProductionReadiness;

/// <summary>
/// Hermetic production-readiness checks: provoke at test time what would otherwise only
/// surface at scale — concurrency and (for the provider-specific Redis counter) atomicity.
/// The Redis case is tagged <c>Live</c> so it is excluded from the hermetic PR lane.
/// </summary>
public sealed class CacheAdapterProductionReadinessTests
{
    [Fact]
    public async Task InMemory_Should_RoundTripEveryEntry_When_WrittenConcurrently()
    {
        using var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var store = new InMemoryCacheStore(cache);

        const int count = 100;

        await Task.WhenAll(Enumerable.Range(0, count).Select(index =>
            store.SetAsync($"key-{index}", Encoding.UTF8.GetBytes($"value-{index}"))));

        var reads = await Task.WhenAll(Enumerable.Range(0, count).Select(index =>
            store.GetAsync($"key-{index}")));

        for (var index = 0; index < count; index++)
            reads[index].Should().Equal(Encoding.UTF8.GetBytes($"value-{index}"));
    }

    [Fact]
    [Trait("Category", "Live")]
    public async Task Redis_Should_IncrementAtomically_When_HitConcurrently()
    {
        await using var store = new RedisCacheStore(Options.Create(new RedisCacheOptions
        {
            Configuration = Environment.GetEnvironmentVariable("OUTLET_REDIS") ?? "localhost:6379",
            KeyPrefix = "outlet-test:",
        }));
        var key = $"counter:{Guid.NewGuid():N}";

        const int count = 200;
        await Task.WhenAll(Enumerable.Range(0, count).Select(_ => store.IncrementAsync(key)));

        var total = await store.IncrementAsync(key, 0);

        total.Should().Be(count);
    }
}
