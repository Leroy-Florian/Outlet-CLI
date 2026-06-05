using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Resolves an item and its <c>registryDependencies</c> into the ordered,
/// de-duplicated install set (dependencies first). Shared by the resolution query
/// (HIJ-491) and the install orchestration (HIJ-496) so the graph logic lives once.
/// </summary>
public sealed class RegistryDependencyResolver(IRegistryClient registryClient)
{
    public async Task<Result<IReadOnlyList<RegistryItem>>> ResolveAsync(string itemName, CancellationToken cancellationToken = default)
    {
        RegistryItemId rootId;
        try
        {
            rootId = RegistryItemId.From(itemName);
        }
        catch (ArgumentException ex)
        {
            return Result<IReadOnlyList<RegistryItem>>.Failure($"Invalid item name '{itemName}': {ex.Message}");
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
