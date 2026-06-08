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
    Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>One item by id, or null when this source does not publish it.</summary>
    Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default);

    Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default);
}
