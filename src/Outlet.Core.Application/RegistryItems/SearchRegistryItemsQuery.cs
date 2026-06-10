namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Query: find catalogue items whose name or concern contains <paramref name="Text"/>
/// (case-insensitive substring). The discoverable counterpart to <c>list</c> — you search
/// when you don't yet know the exact item id.
/// </summary>
public sealed record SearchRegistryItemsQuery(string Text);
