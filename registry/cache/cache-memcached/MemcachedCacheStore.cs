using Enyim.Caching;
using Enyim.Caching.Memcached;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Cache;

/// <summary>
/// Memcached adapter for <see cref="ICacheStore"/>, backed by EnyimMemcachedCore. Thin by
/// design: resilience (retry / circuit breaker) is composed over the port, never embedded
/// here. Values are stored as raw bytes by the default transcoder, so the payload survives
/// a round-trip unchanged.
/// </summary>
public sealed class MemcachedCacheStore(IMemcachedClient client, IOptions<MemcachedCacheOptions> optionsAccessor) : ICacheStore
{
    private readonly MemcachedCacheOptions _options = optionsAccessor.Value;

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await client.GetAsync<byte[]>(Qualify(key));
        return result.Success ? result.Value : null;
    }

    public async Task SetAsync(string key, byte[] value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // memcached treats a zero TimeSpan as "no expiry"; that is our null-TTL case too.
        var ttl = (options ?? CacheEntryOptions.None).TimeToLive ?? TimeSpan.Zero;
        await client.StoreAsync(StoreMode.Set, Qualify(key), value, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await client.RemoveAsync(Qualify(key));
    }

    private string Qualify(string key) => string.IsNullOrEmpty(_options.KeyPrefix) ? key : _options.KeyPrefix + key;
}
