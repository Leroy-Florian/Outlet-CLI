namespace Outlet.Registry.Storage;

/// <summary>
/// Generic object/blob storage port — identical across every adapter so providers
/// (in-memory, filesystem, S3, Azure Blob…) stay swappable. No provider specifics ever
/// leak into this interface; a provider-specific feature (e.g. presigned URLs) goes on a
/// dedicated companion interface implemented by the same adapter.
/// </summary>
/// <remarks>
/// Conventions shared by every adapter:
/// <list type="bullet">
/// <item>A <b>key</b> is an opaque, case-sensitive path-like identifier (e.g. <c>invoices/2026/03.pdf</c>).</item>
/// <item><b>Absence is not an error</b>: <see cref="GetAsync"/> returns <c>null</c> and
/// <see cref="DeleteAsync"/> returns <c>false</c> when the object does not exist — never an exception.</item>
/// <item>Genuine I/O faults (network, permissions) propagate as exceptions, as is idiomatic for storage.</item>
/// </list>
/// </remarks>
public interface IBlobStorage
{
    /// <summary>Stores <paramref name="content"/> under <paramref name="key"/>, overwriting any existing object.</summary>
    Task PutAsync(string key, Stream content, PutBlobOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the object at <paramref name="key"/>, or <c>null</c> when it does not exist.
    /// The returned <see cref="Blob"/> owns a stream the caller must dispose
    /// (<c>await using</c>).
    /// </summary>
    Task<Blob?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns whether an object exists at <paramref name="key"/>.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes the object at <paramref name="key"/>; returns <c>true</c> if one was removed, <c>false</c> if none existed.</summary>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Lists object metadata, optionally restricted to keys starting with <paramref name="prefix"/>.</summary>
    IAsyncEnumerable<BlobInfo> ListAsync(string? prefix = null, CancellationToken cancellationToken = default);
}
