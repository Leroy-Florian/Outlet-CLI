using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Storage;

/// <summary>
/// In-memory adapter for <see cref="IBlobStorage"/> — a thread-safe dictionary of byte
/// payloads. Ideal for tests, local development and ephemeral caches; nothing is persisted
/// beyond the process. Thin by design: resilience is composed over the port, never embedded.
/// </summary>
public sealed class InMemoryBlobStorage : IBlobStorage
{
    private readonly ConcurrentDictionary<string, Entry> _objects;
    private readonly StringComparison _keyComparison;

    // non-primary: the key comparer is derived from options before the field is initialised.
    public InMemoryBlobStorage(IOptions<InMemoryBlobStorageOptions> options)
    {
        var caseSensitive = options.Value.CaseSensitiveKeys;
        _keyComparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        _objects = new ConcurrentDictionary<string, Entry>(
            caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
    }

    public Task PutAsync(string key, Stream content, PutBlobOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = new MemoryStream();
        content.CopyTo(buffer);

        var metadata = options?.Metadata is { Count: > 0 } supplied
            ? new Dictionary<string, string>(supplied)
            : [];

        _objects[key] = new Entry(buffer.ToArray(), options?.ContentType, metadata, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task<Blob?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_objects.TryGetValue(key, out var entry))
            return Task.FromResult<Blob?>(null);

        var stream = new MemoryStream(entry.Content, writable: false);
        return Task.FromResult<Blob?>(new Blob(entry.ToInfo(key), stream));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_objects.ContainsKey(key));
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_objects.TryRemove(key, out _));
    }

    public async IAsyncEnumerable<BlobInfo> ListAsync(
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (key, entry) in _objects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (prefix is { Length: > 0 } && !key.StartsWith(prefix, _keyComparison))
                continue;

            yield return entry.ToInfo(key);
        }

        await Task.CompletedTask;
    }

    private sealed record Entry(byte[] Content, string? ContentType, IReadOnlyDictionary<string, string> Metadata, DateTimeOffset LastModified)
    {
        public BlobInfo ToInfo(string key) => new()
        {
            Key = key,
            Size = Content.Length,
            ContentType = ContentType,
            ETag = null,
            LastModified = LastModified,
            Metadata = Metadata,
        };
    }
}
