namespace Outlet.Registry.Cache;

/// <summary>
/// Provider-specific companion to <see cref="ICacheStore"/>. It lives BESIDE the generic
/// port (never inside it) and is implemented by the very same adapter instance, so generic
/// code stays swappable while Redis-only capabilities (atomic counters) remain reachable
/// when you opt in.
/// </summary>
public interface IRedisCacheStore : ICacheStore
{
    /// <summary>
    /// Atomically increments the integer stored at <paramref name="key"/> by
    /// <paramref name="by"/> and returns the new value (Redis INCR/INCRBY).
    /// </summary>
    Task<long> IncrementAsync(string key, long by = 1, CancellationToken cancellationToken = default);
}
