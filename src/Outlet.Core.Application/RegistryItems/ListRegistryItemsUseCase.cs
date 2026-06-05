using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Lists the items available across the configured registries,
/// optionally filtered by concern.
/// </summary>
public sealed class ListRegistryItemsUseCase(IRegistryClient registryClient)
    : IUseCase<ListRegistryItemsQuery, IReadOnlyList<RegistryItemSummary>>
{
    public async Task<Result<IReadOnlyList<RegistryItemSummary>>> HandleAsync(
        ListRegistryItemsQuery command,
        CancellationToken cancellationToken = default)
    {
        var items = await registryClient.GetItemsAsync(cancellationToken);

        var filtered = command.Concern is null
            ? items
            : [.. items.Where(i => i.Concern.Value == command.Concern)];

        IReadOnlyList<RegistryItemSummary> summaries =
        [
            .. filtered.Select(i => new RegistryItemSummary(
                i.Id.Value,
                i.Concern.Value,
                i.Type == Domain.RegistryItems.RegistryItemType.Contract ? "outlet:contract" : "outlet:adapter",
                i.Files.Count)),
        ];

        return Result<IReadOnlyList<RegistryItemSummary>>.Success(summaries);
    }
}
