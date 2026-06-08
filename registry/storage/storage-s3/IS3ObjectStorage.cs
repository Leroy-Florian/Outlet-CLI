namespace Outlet.Registry.Storage;

/// <summary>
/// Provider-specific companion to <see cref="IBlobStorage"/>. It lives BESIDE the generic
/// port (never inside it) and is implemented by the very same adapter instance, so generic
/// code stays swappable while S3-only features (presigned URLs) remain reachable when you opt in.
/// </summary>
public interface IS3ObjectStorage : IBlobStorage
{
    /// <summary>
    /// Builds a time-limited, pre-authenticated GET URL for <paramref name="key"/> — share it so a
    /// client can download the object directly from S3 without AWS credentials. Purely local: no
    /// network call, so the object need not exist yet.
    /// </summary>
    Uri GeneratePresignedGetUrl(string key, TimeSpan expiresIn);
}
