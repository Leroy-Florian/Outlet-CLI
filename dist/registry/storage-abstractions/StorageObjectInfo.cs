namespace Outlet.Registry.Storage;

/// <summary>
/// Metadata describing a stored object (no payload). Returned by listing and carried by
/// <see cref="StorageObject.Info"/>. <see cref="ETag"/> and <see cref="LastModified"/> are
/// best-effort: an adapter leaves them null when its backend exposes no equivalent.
/// </summary>
public sealed record StorageObjectInfo
{
    public required string Key { get; init; }

    /// <summary>Size of the stored payload in bytes.</summary>
    public long Size { get; init; }

    /// <summary>MIME content type recorded at write time, when the backend preserves it.</summary>
    public string? ContentType { get; init; }

    /// <summary>Provider entity tag (version/content marker) when available.</summary>
    public string? ETag { get; init; }

    /// <summary>Last write time when the backend reports one.</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>Caller-supplied metadata stored alongside the object (empty when none).</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
