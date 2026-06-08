using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Storage;

/// <summary>
/// Local-filesystem adapter for <see cref="IBlobStorage"/>. Keys map to paths under a
/// configured root; content type and caller metadata are persisted in a sibling sidecar
/// tree (<c>.outlet-meta/</c>) so the object payload stays byte-for-byte identical. Thin by
/// design: resilience is composed over the port, never embedded here.
/// </summary>
public sealed class FileSystemBlobStorage(IOptions<FileSystemBlobStorageOptions> options) : IBlobStorage
{
    private const string MetaDirectory = ".outlet-meta";
    private readonly string _root = Path.GetFullPath(
        string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? throw new InvalidOperationException($"{nameof(FileSystemBlobStorageOptions)}.{nameof(FileSystemBlobStorageOptions.RootPath)} must be set.")
            : options.Value.RootPath);

    public async Task PutAsync(string key, Stream content, PutBlobOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var contentPath = ResolveContentPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(contentPath)!);

        await using (var file = new FileStream(contentPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            await content.CopyToAsync(file, cancellationToken);

        await WriteMetadataAsync(key, options, cancellationToken);
    }

    public async Task<Blob?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var contentPath = ResolveContentPath(key);
        if (!File.Exists(contentPath))
            return null;

        var metadata = await ReadMetadataAsync(key, cancellationToken);
        try
        {
            var stream = new FileStream(contentPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            return new Blob(BuildInfo(key, new FileInfo(contentPath), metadata), stream);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Raced with a concurrent delete — treat as absent.
            return null;
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(ResolveContentPath(key)));
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contentPath = ResolveContentPath(key);
        if (!File.Exists(contentPath))
            return Task.FromResult(false);

        File.Delete(contentPath);

        var metaPath = ResolveMetadataPath(key);
        if (File.Exists(metaPath))
            File.Delete(metaPath);

        return Task.FromResult(true);
    }

    public async IAsyncEnumerable<BlobInfo> ListAsync(
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_root))
            yield break;

        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = Path.GetRelativePath(_root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (key.StartsWith(MetaDirectory + "/", StringComparison.Ordinal))
                continue;
            if (prefix is { Length: > 0 } && !key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var metadata = await ReadMetadataAsync(key, cancellationToken);
            yield return BuildInfo(key, new FileInfo(file), metadata);
        }
    }

    private static BlobInfo BuildInfo(string key, FileInfo file, StoredMetadata? metadata) => new()
    {
        Key = key,
        Size = file.Length,
        ContentType = metadata?.ContentType,
        ETag = null,
        LastModified = file.LastWriteTimeUtc,
        Metadata = metadata?.Metadata ?? [],
    };

    private async Task WriteMetadataAsync(string key, PutBlobOptions? options, CancellationToken cancellationToken)
    {
        var metaPath = ResolveMetadataPath(key);

        var hasMetadata = options is not null
            && (options.ContentType is not null || options.Metadata.Count > 0);

        if (!hasMetadata)
        {
            // Overwrite resets metadata: drop any stale sidecar.
            if (File.Exists(metaPath))
                File.Delete(metaPath);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(metaPath)!);
        var stored = new StoredMetadata
        {
            ContentType = options!.ContentType,
            Metadata = new Dictionary<string, string>(options.Metadata),
        };
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(stored), cancellationToken);
    }

    private async Task<StoredMetadata?> ReadMetadataAsync(string key, CancellationToken cancellationToken)
    {
        var metaPath = ResolveMetadataPath(key);
        if (!File.Exists(metaPath))
            return null;

        var json = await File.ReadAllTextAsync(metaPath, cancellationToken);
        return JsonSerializer.Deserialize<StoredMetadata>(json);
    }

    private string ResolveContentPath(string key) => Path.Combine(_root, NormalizeKey(key));

    private string ResolveMetadataPath(string key) => Path.Combine(_root, MetaDirectory, NormalizeKey(key) + ".json");

    private static string NormalizeKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var segments = key.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || Array.Exists(segments, s => s is "." or ".."))
            throw new ArgumentException($"Invalid storage key '{key}': path traversal is not allowed.", nameof(key));

        return Path.Combine(segments);
    }

    private sealed record StoredMetadata
    {
        public string? ContentType { get; init; }
        public Dictionary<string, string> Metadata { get; init; } = [];
    }
}
