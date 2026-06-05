namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Query: resolve a registry item and all of its registry dependencies into the
/// ordered, de-duplicated list of items to install (dependencies first).
/// </summary>
public sealed record ResolveItemDependenciesQuery(string ItemName);
