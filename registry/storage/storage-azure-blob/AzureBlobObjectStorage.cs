using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Storage;

/// <summary>
/// Azure Blob Storage adapter implementing both the generic <see cref="IBlobStorage"/> and
/// the provider-specific <see cref="IAzureBlobStorage"/> from one class — registered once and
/// forwarded to both interfaces (see AddAzureBlobStorage). Thin by design: resilience is
/// composed over the port, never embedded here.
/// </summary>
public sealed class AzureBlobObjectStorage : IAzureBlobStorage
{
    private readonly BlobContainerClient _container;
    private readonly bool _createIfNotExists;
    private readonly Lazy<Task> _ensureContainer;

    // non-primary: the container client is assembled from options before fields are initialised.
    public AzureBlobObjectStorage(IOptions<AzureBlobObjectStorageOptions> options)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.ConnectionString))
            throw new InvalidOperationException($"{nameof(AzureBlobObjectStorageOptions)}.{nameof(AzureBlobObjectStorageOptions.ConnectionString)} must be set.");
        if (string.IsNullOrWhiteSpace(value.ContainerName))
            throw new InvalidOperationException($"{nameof(AzureBlobObjectStorageOptions)}.{nameof(AzureBlobObjectStorageOptions.ContainerName)} must be set.");

        _container = new BlobContainerClient(value.ConnectionString, value.ContainerName);
        _createIfNotExists = value.CreateContainerIfNotExists;
        _ensureContainer = new Lazy<Task>(() => _container.CreateIfNotExistsAsync());
    }

    public async Task PutAsync(string key, Stream content, PutObjectOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        await EnsureContainerAsync();

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = options?.ContentType },
        };

        if (options?.Metadata is { Count: > 0 } metadata)
            uploadOptions.Metadata = new Dictionary<string, string>(metadata);

        await _container.GetBlobClient(key).UploadAsync(content, uploadOptions, cancellationToken);
    }

    public async Task<StorageObject?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.GetBlobClient(key).DownloadStreamingAsync(cancellationToken: cancellationToken);
            var result = response.Value;
            return new StorageObject(BuildInfo(key, result.Details), result.Content);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => await _container.GetBlobClient(key).ExistsAsync(cancellationToken);

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        => await _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken))
        {
            yield return new StorageObjectInfo
            {
                Key = blob.Name,
                Size = blob.Properties.ContentLength ?? 0,
                ContentType = blob.Properties.ContentType,
                ETag = blob.Properties.ETag?.ToString(),
                LastModified = blob.Properties.LastModified,
            };
        }
    }

    public Uri GenerateReadSasUri(string key, TimeSpan expiresIn)
    {
        var blob = _container.GetBlobClient(key);
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "A read SAS URI requires an account-key connection string; the adapter was configured without one.");

        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(expiresIn));
    }

    private Task EnsureContainerAsync() => _createIfNotExists ? _ensureContainer.Value : Task.CompletedTask;

    private static StorageObjectInfo BuildInfo(string key, BlobDownloadDetails details) => new()
    {
        Key = key,
        Size = details.ContentLength,
        ContentType = details.ContentType,
        ETag = details.ETag.ToString(),
        LastModified = details.LastModified,
        Metadata = details.Metadata is { Count: > 0 }
            ? new Dictionary<string, string>(details.Metadata)
            : [],
    };
}
