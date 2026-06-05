namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Query: list every item available across the configured registries,
/// optionally filtered by concern (e.g. "email").
/// </summary>
public sealed record ListRegistryItemsQuery(string? Concern = null);
