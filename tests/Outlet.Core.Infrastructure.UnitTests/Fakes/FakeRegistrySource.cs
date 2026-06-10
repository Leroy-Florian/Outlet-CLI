using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.Registry;

namespace Outlet.Core.Infrastructure.UnitTests.Fakes;

/// <summary>Hand-written in-memory <see cref="IRegistrySource"/> for composition tests.</summary>
public sealed class FakeRegistrySource(string name = "fake") : IRegistrySource
{
    private readonly Dictionary<string, RegistryItem> _items = [];
    private readonly Dictionary<(string Item, string Path), string> _files = [];

    public string Name => name;

    public FakeRegistrySource Add(RegistryItem item)
    {
        _items[item.Id.Value] = item;
        return this;
    }

    public FakeRegistrySource AddFile(string itemId, string path, string content)
    {
        _files[(itemId, path)] = content;
        return this;
    }

    public Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RegistryItem>>([.. _items.Values]);

    public Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.GetValueOrDefault(id.Value));

    public Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default)
        => _files.TryGetValue((id.Value, filePath), out var content)
            ? Task.FromResult(content)
            : throw new InvalidOperationException($"No file seeded for {id.Value}/{filePath}.");
}
