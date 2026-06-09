namespace Outlet.Registry.Cache;

/// <summary>Options for the Memcached adapter, bound via <c>IOptions&lt;MemcachedCacheOptions&gt;</c>.</summary>
public sealed class MemcachedCacheOptions
{
    /// <summary>Memcached server host name or IP.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Memcached server port (memcached's default is 11211).</summary>
    public int Port { get; set; } = 11211;

    /// <summary>Optional logical key prefix prepended to every key (key namespacing). Empty = none.</summary>
    public string KeyPrefix { get; set; } = "";
}
