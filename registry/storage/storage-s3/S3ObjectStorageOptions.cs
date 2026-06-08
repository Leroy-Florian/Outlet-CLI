namespace Outlet.Registry.Storage;

/// <summary>Options for the AWS S3 adapter, bound via <c>IOptions&lt;S3ObjectStorageOptions&gt;</c>.</summary>
public sealed class S3ObjectStorageOptions
{
    /// <summary>Target bucket. Required.</summary>
    public string BucketName { get; set; } = "";

    /// <summary>AWS region system name (e.g. <c>eu-west-1</c>). Ignored when <see cref="ServiceUrl"/> is set.</summary>
    public string? Region { get; set; }

    /// <summary>
    /// Override the service endpoint — point the adapter at MinIO / an S3-compatible store, or a
    /// local stub in tests. When set, you usually also want <see cref="ForcePathStyle"/> = true.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Use path-style addressing (<c>host/bucket/key</c>) instead of virtual-hosted. Required by most S3-compatible stores.</summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>Static access key. Leave null to use the default AWS credential chain (env, profile, instance role…).</summary>
    public string? AccessKeyId { get; set; }

    /// <summary>Static secret key. Leave null to use the default AWS credential chain.</summary>
    public string? SecretAccessKey { get; set; }
}
