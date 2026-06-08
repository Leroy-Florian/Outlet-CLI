namespace Outlet.Registry.Storage;

/// <summary>
/// Provider-specific companion to <see cref="IBlobStorage"/>. It lives BESIDE the generic
/// port (never inside it) and is implemented by the very same adapter instance, so generic
/// code stays swappable while Azure-only features (SAS URIs) remain reachable when you opt in.
/// </summary>
public interface IAzureBlobStorage : IBlobStorage
{
    /// <summary>
    /// Builds a time-limited, read-only SAS URI for <paramref name="key"/> — share it so a client
    /// can download the blob directly without storage credentials. Requires the adapter to have been
    /// configured with an account-key connection string.
    /// </summary>
    Uri GenerateReadSasUri(string key, TimeSpan expiresIn);
}
