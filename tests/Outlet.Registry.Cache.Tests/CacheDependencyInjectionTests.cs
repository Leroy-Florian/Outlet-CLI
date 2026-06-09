using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Cache;

namespace Outlet.Registry.Cache.Tests;

public sealed class CacheDependencyInjectionTests
{
    [Fact]
    public void Should_ResolveInMemoryStore_When_AddInMemoryCacheIsCalled()
    {
        var services = new ServiceCollection();
        services.AddInMemoryCache();

        using var provider = services.BuildServiceProvider();
        var store = provider.GetService<ICacheStore>();

        store.Should().BeOfType<InMemoryCacheStore>();
    }

    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddRedisCacheIsCalled()
    {
        var services = new ServiceCollection();
        services.AddRedisCache(o => o.Configuration = "localhost:6379");

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<ICacheStore>();
        var specific = provider.GetRequiredService<IRedisCacheStore>();

        generic.Should().BeOfType<RedisCacheStore>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_ResolveMemcachedStore_When_AddMemcachedCacheIsCalled()
    {
        var services = new ServiceCollection();
        services.AddMemcachedCache(o =>
        {
            o.Host = "localhost";
            o.Port = 11211;
        });

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ICacheStore>();

        store.Should().BeOfType<MemcachedCacheStore>();
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        ICacheStore Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider().GetRequiredService<ICacheStore>();
        }

        var memory = Resolve(s => s.AddInMemoryCache());
        var redis = Resolve(s => s.AddRedisCache(o => o.Configuration = "localhost:6379"));
        var memcached = Resolve(s => s.AddMemcachedCache(o => o.Host = "localhost"));

        memory.Should().BeOfType<InMemoryCacheStore>();
        redis.Should().BeOfType<RedisCacheStore>();
        memcached.Should().BeOfType<MemcachedCacheStore>();
    }
}
