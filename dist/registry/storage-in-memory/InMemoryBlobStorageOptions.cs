namespace Outlet.Registry.Storage;

/// <summary>Options for the in-memory adapter, bound via <c>IOptions&lt;InMemoryBlobStorageOptions&gt;</c>.</summary>
public sealed class InMemoryBlobStorageOptions
{
    /// <summary>
    /// Whether keys are compared case-sensitively (the default, matching S3/Azure).
    /// Set false to mimic a case-insensitive backend.
    /// </summary>
    public bool CaseSensitiveKeys { get; set; } = true;
}
