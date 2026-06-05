using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Resolves an item and its <c>registryDependencies</c> recursively into the
/// ordered, de-duplicated install set (each dependency appears before the items
/// that need it). Fails cleanly on a missing item or a dependency cycle —
/// expected errors are returned as <see cref="Result"/>, never thrown.
/// </summary>
public sealed class ResolveItemDependenciesUseCase(IRegistryClient registryClient)
    : IUseCase<ResolveItemDependenciesQuery, IReadOnlyList<RegistryItem>>
{
    public async Task<Result<IReadOnlyList<RegistryItem>>> HandleAsync(
        ResolveItemDependenciesQuery command,
        CancellationToken cancellationToken = default)
    {
        RegistryItemId rootId;
        try
        {
            rootId = RegistryItemId.From(command.ItemName);
        }
        catch (ArgumentException ex)
        {
            return Result<IReadOnlyList<RegistryItem>>.Failure(
                $"Invalid item name '{command.ItemName}': {ex.Message}");
        }

        var ordered = new List<RegistryItem>();
        var resolved = new HashSet<string>();
        var onPath = new HashSet<string>();

        var error = await VisitAsync(rootId, ordered, resolved, onPath, cancellationToken);

        return error is null
            ? Result<IReadOnlyList<RegistryItem>>.Success(ordered)
            : Result<IReadOnlyList<RegistryItem>>.Failure(error);
    }

    private async Task<string?> VisitAsync(
        RegistryItemId id,
        List<RegistryItem> ordered,
        HashSet<string> resolved,
        HashSet<string> onPath,
        CancellationToken cancellationToken)
    {
        if (resolved.Contains(id.Value))
            return null;

        if (!onPath.Add(id.Value))
            return $"Cyclic registry dependency detected at '{id.Value}'.";

        var item = await registryClient.GetItemAsync(id, cancellationToken);
        if (item is null)
            return $"Registry item '{id.Value}' was not found in any configured registry.";

        foreach (var dependency in item.RegistryDependencies)
        {
            var error = await VisitAsync(dependency, ordered, resolved, onPath, cancellationToken);
            if (error is not null)
                return error;
        }

        onPath.Remove(id.Value);
        resolved.Add(id.Value);
        ordered.Add(item);
        return null;
    }
}
