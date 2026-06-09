using Microsoft.Extensions.Caching.Memory;

namespace Outlet.Registry.Cache;

/// <summary>
/// In-process adapter for <see cref="ICacheStore"/>, backed by <see cref="IMemoryCache"/>.
/// The local/single-node baseline (no server, fully hermetic) behind the same port the
/// distributed adapters implement — so a process can start on in-memory and graduate to
/// Redis or Memcached by changing one AddXxx line. Every entry declares its byte size, so
/// the cache works whether or not a <see cref="InMemoryCacheOptions.SizeLimit"/> is set.
/// </summary>
public sealed class InMemoryCacheStore(IMemoryCache cache) : ICacheStore
{
    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<byte[]?>(cache.TryGetValue(key, out byte[]? value) ? value : null);
    }

    public Task SetAsync(string key, byte[] value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entryOptions = new MemoryCacheEntryOptions { Size = value.Length };
        if ((options ?? CacheEntryOptions.None).TimeToLive is { } ttl)
            entryOptions.AbsoluteExpirationRelativeToNow = ttl;

        cache.Set(key, value, entryOptions);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        cache.Remove(key);
        return Task.CompletedTask;
    }
}
