using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.Conformance;

/// <summary>
/// AWS S3 adapter run against a real S3-compatible endpoint (level B/C — MinIO emulator or a
/// sandbox bucket). Lives in the non-blocking nightly lane; skipped when no endpoint is set.
/// Configure with <c>OUTLET_S3_TEST_SERVICE_URL</c> (+ optional access/secret/region).
/// </summary>
[Trait("Category", "Live")]
public sealed class S3ObjectStorageConformanceTests : ObjectStorageConformanceTests
{
    protected override async Task<IObjectStorageHarness?> CreateOrSkipAsync()
    {
        var serviceUrl = Environment.GetEnvironmentVariable("OUTLET_S3_TEST_SERVICE_URL");
        if (string.IsNullOrWhiteSpace(serviceUrl))
            return null;

        var accessKey = Environment.GetEnvironmentVariable("OUTLET_S3_TEST_ACCESS_KEY") ?? "minioadmin";
        var secretKey = Environment.GetEnvironmentVariable("OUTLET_S3_TEST_SECRET_KEY") ?? "minioadmin";
        var bucket = "outlet-conformance-" + Guid.NewGuid().ToString("N")[..12];

        var admin = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true });
        await admin.PutBucketAsync(bucket);

        var storage = new S3ObjectStorage(Options.Create(new S3ObjectStorageOptions
        {
            ServiceUrl = serviceUrl,
            ForcePathStyle = true,
            BucketName = bucket,
            AccessKeyId = accessKey,
            SecretAccessKey = secretKey,
        }));

        return new Harness(storage, admin, bucket);
    }

    private sealed class Harness(S3ObjectStorage storage, IAmazonS3 admin, string bucket) : IObjectStorageHarness
    {
        public IObjectStorage Storage => storage;

        public async ValueTask DisposeAsync()
        {
            var listed = await admin.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket });
            foreach (var item in listed.S3Objects ?? [])
                await admin.DeleteObjectAsync(bucket, item.Key);

            await admin.DeleteBucketAsync(bucket);
            storage.Dispose();
            admin.Dispose();
        }
    }
}
