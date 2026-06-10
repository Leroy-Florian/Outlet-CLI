using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Searches the items across the configured registries by a free-text fragment,
/// matched (case-insensitively) against the item id and its concern.
/// </summary>
public sealed class SearchRegistryItemsUseCase(IRegistryClient registryClient)
    : IUseCase<SearchRegistryItemsQuery, IReadOnlyList<RegistryItemSummary>>
{
    public async Task<Result<IReadOnlyList<RegistryItemSummary>>> HandleAsync(
        SearchRegistryItemsQuery command,
        CancellationToken cancellationToken = default)
    {
        var text = command.Text?.Trim() ?? string.Empty;
        var items = await registryClient.GetItemsAsync(cancellationToken);

        IReadOnlyList<RegistryItemSummary> summaries =
        [
            .. items
                .Where(i => Matches(i, text))
                .OrderBy(i => i.Id.Value, StringComparer.Ordinal)
                .Select(i => new RegistryItemSummary(
                    i.Id.Value,
                    i.Concern.Value,
                    i.Type == RegistryItemType.Contract ? "outlet:contract" : "outlet:adapter",
                    i.Files.Count)),
        ];

        return Result<IReadOnlyList<RegistryItemSummary>>.Success(summaries);
    }

    private static bool Matches(RegistryItem item, string text)
        => text.Length == 0
        || item.Id.Value.Contains(text, StringComparison.OrdinalIgnoreCase)
        || item.Concern.Value.Contains(text, StringComparison.OrdinalIgnoreCase);
}
