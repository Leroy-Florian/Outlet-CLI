namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Read-model DTO returned by <see cref="ListRegistryItemsUseCase"/> —
/// primitives only, mapped at the boundary for CLI display.
/// </summary>
public sealed record RegistryItemSummary(
    string Name,
    string Concern,
    string Type,
    int FileCount,
    string Version);
