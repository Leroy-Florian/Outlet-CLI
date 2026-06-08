namespace Outlet.Registry.Storage;

/// <summary>
/// A fetched object: its <see cref="Info"/> plus a readable <see cref="Content"/> stream the
/// caller owns. Dispose it (<c>await using</c>) to release the underlying file handle / HTTP
/// response. The common ~80% case is "read the whole thing"; for very large payloads, stream
/// <see cref="Content"/> rather than buffering it.
/// </summary>
public sealed class StorageObject(StorageObjectInfo info, Stream content) : IAsyncDisposable, IDisposable
{
    public StorageObjectInfo Info { get; } = info;

    /// <summary>The object's payload. Readable, positioned at the start, owned by the caller.</summary>
    public Stream Content { get; } = content;

    public void Dispose() => Content.Dispose();

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
