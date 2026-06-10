using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>
/// Hand-written in-memory fake of <see cref="IRegistryClient"/> —
/// no mocking framework, per the testing strategy.
/// </summary>
public sealed class FakeRegistryClient : IRegistryClient
{
    private readonly Dictionary<string, RegistryItem> _items = [];
    private readonly Dictionary<(string ItemId, string FilePath), string> _fileContents = [];

    /// <summary>The registry name reported as the provenance of every seeded item (default: the trusted official registry).</summary>
    public string SourceName { get; set; } = "outlet";

    public void Seed(RegistryItem item) => _items[item.Id.Value] = item;

    public void SeedFileContent(RegistryItemId id, string filePath, string content)
        => _fileContents[(id.Value, filePath)] = content;

    public Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RegistryItem>>([.. _items.Values]);

    public Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.GetValueOrDefault(id.Value));

    public Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default)
        => _fileContents.TryGetValue((id.Value, filePath), out var content)
            ? Task.FromResult(content)
            : throw new InvalidOperationException($"No file content seeded for {id}/{filePath}.");

    public Task<string?> GetSourceNameAsync(RegistryItemId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.ContainsKey(id.Value) ? SourceName : null);
}
