namespace Outlet.Registry.Cache;

/// <summary>Options for the Redis adapter, bound via <c>IOptions&lt;RedisCacheOptions&gt;</c>.</summary>
public sealed class RedisCacheOptions
{
    /// <summary>
    /// StackExchange.Redis configuration string — typically <c>"host:port"</c>
    /// (e.g. <c>"localhost:6379"</c>) or a full comma-separated configuration string.
    /// </summary>
    public string Configuration { get; set; } = "";

    /// <summary>Optional logical key prefix prepended to every key (key namespacing). Empty = none.</summary>
    public string KeyPrefix { get; set; } = "";
}
