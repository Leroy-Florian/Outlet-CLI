using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// A SINGLE registry endpoint (the public Outlet registry, or a private company
/// one). <see cref="MultiSourceRegistryClient"/> fans the Application port
/// <c>IRegistryClient</c> out across several of these.
///
/// Authentication (token/header) is out of scope for the MVP but the shape must
/// not preclude it — a future authenticated source implements this same interface.
/// </summary>
public interface IRegistrySource
{
    /// <summary>Every item this source publishes.</summary>
    Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>One item by id, or null when this source does not publish it.</summary>
    Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default);

    /// <summary>Raw content of one file of an item served by this source.</summary>
    Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default);
}
