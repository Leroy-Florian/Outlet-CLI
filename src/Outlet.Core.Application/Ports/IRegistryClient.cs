using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — fetches the registry manifest and item files over HTTP.
/// Multi-source by design: a project can point at several registries
/// (public Outlet + private company registries).
/// </summary>
public interface IRegistryClient
{
    /// <summary>Fetches every item declared by the configured registries.</summary>
    Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetches a single item by id, or null when no registry declares it.</summary>
    Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default);

    /// <summary>Downloads the raw content of one file of an item.</summary>
    Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default);
}
