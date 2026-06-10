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

    Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// The configured name of the registry that serves <paramref name="id"/> (first source wins,
    /// mirroring resolution), or null when no source provides it. Lets a caller decide whether the
    /// code about to be copied in comes from a registry the user has marked trusted.
    /// </summary>
    Task<string?> GetSourceNameAsync(RegistryItemId id, CancellationToken cancellationToken = default);
}
