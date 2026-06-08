namespace Outlet.Registry.Storage;

/// <summary>
/// Optional write metadata for <see cref="IBlobStorage.PutAsync"/>. Everything here is
/// the common ~80% case; for provider-only knobs (storage class, encryption headers…),
/// use the adapter's companion interface or edit your owned copy.
/// </summary>
public sealed class PutBlobOptions
{
    /// <summary>MIME type recorded with the object (e.g. <c>application/pdf</c>).</summary>
    public string? ContentType { get; set; }

    /// <summary>Caller-defined metadata persisted alongside the object.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
