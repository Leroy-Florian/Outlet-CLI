using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// SECONDARY ADAPTER — implements the Application port <c>IRegistryClient</c> by
/// fanning out across the configured <see cref="IRegistrySource"/>s. Sources are
/// consulted in order; on an item-name collision the first source wins (a project
/// can shadow a public item with a private one by ordering its registries).
/// </summary>
public sealed class MultiSourceRegistryClient(IEnumerable<IRegistrySource> sources) : IRegistryClient
{
    private readonly IReadOnlyList<IRegistrySource> _sources = [.. sources];

    public async Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        var byId = new Dictionary<string, RegistryItem>();

        foreach (var source in _sources)
        foreach (var item in await source.GetItemsAsync(cancellationToken))
            byId.TryAdd(item.Id.Value, item);

        return [.. byId.Values];
    }

    public async Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default)
    {
        foreach (var source in _sources)
        {
            var item = await source.GetItemAsync(id, cancellationToken);
            if (item is not null)
                return item;
        }

        return null;
    }

    public async Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default)
    {
        foreach (var source in _sources)
        {
            var item = await source.GetItemAsync(id, cancellationToken);
            if (item is not null)
                return await source.GetFileContentAsync(id, filePath, cancellationToken);
        }

        throw new InvalidOperationException($"No configured registry provides item '{id.Value}'.");
    }
}
