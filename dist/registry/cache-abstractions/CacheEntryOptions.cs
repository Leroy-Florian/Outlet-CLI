namespace Outlet.Registry.Cache;

/// <summary>
/// Generic expiration policy — the common ~80% case (no god-model). Only an absolute
/// time-to-live is exposed because it is the ONE policy every backend honours uniformly
/// (Redis SET EX, memcached expiry, in-memory absolute expiration), which is what keeps
/// adapters truly swappable. Backend-only knobs (sliding windows, tags, regions, eviction
/// priority…) belong on a companion interface or in your owned copy — never here.
/// </summary>
public sealed record CacheEntryOptions
{
    /// <summary>Time-to-live applied when the entry is written. Null = cache until evicted (no expiry).</summary>
    public TimeSpan? TimeToLive { get; init; }

    /// <summary>No expiration — cache until the backend evicts.</summary>
    public static CacheEntryOptions None { get; } = new();

    /// <summary>Convenience: expire <paramref name="ttl"/> after the entry is written.</summary>
    public static CacheEntryOptions ExpiresIn(TimeSpan ttl) => new() { TimeToLive = ttl };
}
