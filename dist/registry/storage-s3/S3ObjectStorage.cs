using System.Net;
using System.Runtime.CompilerServices;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Storage;

/// <summary>
/// AWS S3 adapter implementing both the generic <see cref="IObjectStorage"/> and the
/// provider-specific <see cref="IS3ObjectStorage"/> from one class — registered once and
/// forwarded to both interfaces (see AddS3ObjectStorage). Thin by design: resilience is
/// composed over the port, never embedded here.
/// </summary>
public sealed class S3ObjectStorage : IS3ObjectStorage, IDisposable
{
    private const string MetadataPrefix = "x-amz-meta-";

    private readonly IAmazonS3 _client;
    private readonly string _bucket;

    // non-primary: the S3 client is assembled from options before fields are initialised.
    public S3ObjectStorage(IOptions<S3ObjectStorageOptions> options)
    {
        var value = options.Value;
        _bucket = string.IsNullOrWhiteSpace(value.BucketName)
            ? throw new InvalidOperationException($"{nameof(S3ObjectStorageOptions)}.{nameof(S3ObjectStorageOptions.BucketName)} must be set.")
            : value.BucketName;

        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(value.ServiceUrl))
        {
            config.ServiceURL = value.ServiceUrl;
            config.ForcePathStyle = value.ForcePathStyle;
        }
        else if (!string.IsNullOrWhiteSpace(value.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(value.Region);
        }

        _client = value is { AccessKeyId: { Length: > 0 } accessKey, SecretAccessKey: { Length: > 0 } secretKey }
            ? new AmazonS3Client(accessKey, secretKey, config)
            : new AmazonS3Client(config);
    }

    public async Task PutAsync(string key, Stream content, PutObjectOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = options?.ContentType,
        };

        if (options?.Metadata is { Count: > 0 } metadata)
        {
            foreach (var (name, metaValue) in metadata)
                request.Metadata.Add(name, metaValue);
        }

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<StorageObject?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucket, key, cancellationToken);
            return new StorageObject(BuildInfo(key, response), response.ResponseStream);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_bucket, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        // S3 DELETE is idempotent (always 204); probe first so the port's "false when absent" holds.
        if (!await ExistsAsync(key, cancellationToken))
            return false;

        await _client.DeleteObjectAsync(_bucket, key, cancellationToken);
        return true;
    }

    public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix };

        do
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken);
            foreach (var item in response.S3Objects ?? [])
            {
                yield return new StorageObjectInfo
                {
                    Key = item.Key,
                    Size = item.Size ?? 0,
                    ETag = item.ETag,
                    LastModified = item.LastModified is { } modified
                        ? new DateTimeOffset(DateTime.SpecifyKind(modified, DateTimeKind.Utc))
                        : null,
                };
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken is not null);
    }

    public Uri GeneratePresignedGetUrl(string key, TimeSpan expiresIn)
    {
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn),
        });

        return new Uri(url);
    }

    public void Dispose() => _client.Dispose();

    private static StorageObjectInfo BuildInfo(string key, GetObjectResponse response)
    {
        var metadata = new Dictionary<string, string>();
        foreach (var metaKey in response.Metadata.Keys)
        {
            var name = metaKey.StartsWith(MetadataPrefix, StringComparison.OrdinalIgnoreCase)
                ? metaKey[MetadataPrefix.Length..]
                : metaKey;
            metadata[name] = response.Metadata[metaKey];
        }

        return new StorageObjectInfo
        {
            Key = key,
            Size = response.Headers.ContentLength,
            ContentType = response.Headers.ContentType,
            ETag = response.ETag,
            LastModified = response.LastModified is { } modified
                ? new DateTimeOffset(DateTime.SpecifyKind(modified, DateTimeKind.Utc))
                : null,
            Metadata = metadata,
        };
    }
}
