namespace Outlet.Registry.Cache;

/// <summary>
/// Generic cache port — identical across every adapter so providers stay swappable.
/// Values are opaque byte payloads: serialization is a SEPARATE concern composed over
/// this port (never embedded here), exactly as resilience is. A cache MISS is modelled
/// as a null return, not an error. Provider specifics never leak into this interface;
/// a provider-only feature goes on a dedicated companion interface implemented by the
/// same adapter (see IRedisCacheStore).
/// </summary>
public interface ICacheStore
{
    /// <summary>Returns the cached bytes stored under <paramref name="key"/>, or null on a miss.</summary>
    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores <paramref name="value"/> under <paramref name="key"/> using the given
    /// expiration policy. A null <paramref name="options"/> means "cache until evicted".
    /// </summary>
    Task SetAsync(string key, byte[] value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Removes <paramref name="key"/> if present (a no-op when the key is absent).</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
