using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Resolves an item and its <c>registryDependencies</c> recursively into the
/// ordered, de-duplicated install set (each dependency appears before the items
/// that need it). Delegates the graph walk to <see cref="RegistryDependencyResolver"/>.
/// </summary>
public sealed class ResolveItemDependenciesUseCase(IRegistryClient registryClient)
    : IUseCase<ResolveItemDependenciesQuery, IReadOnlyList<RegistryItem>>
{
    private readonly RegistryDependencyResolver _resolver = new(registryClient);

    public Task<Result<IReadOnlyList<RegistryItem>>> HandleAsync(
        ResolveItemDependenciesQuery command,
        CancellationToken cancellationToken = default)
        => _resolver.ResolveAsync(command.ItemName, cancellationToken);
}
