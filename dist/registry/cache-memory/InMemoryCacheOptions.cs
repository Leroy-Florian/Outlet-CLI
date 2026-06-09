namespace Outlet.Registry.Cache;

/// <summary>Options for the in-memory adapter, applied when the backing memory cache is built.</summary>
public sealed class InMemoryCacheOptions
{
    /// <summary>
    /// Optional bound on the total cached size (sum of entry sizes, in bytes — each entry's
    /// size is its payload length). Null leaves the cache unbounded.
    /// </summary>
    public long? SizeLimit { get; set; }
}
