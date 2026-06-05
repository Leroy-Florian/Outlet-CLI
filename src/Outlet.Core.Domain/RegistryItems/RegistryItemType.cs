namespace Outlet.Core.Domain.RegistryItems;

/// <summary>
/// The two kinds of registry items, mirroring the manifest "type" field:
/// - Contract: port + DTOs, zero external dependency (outlet:contract)
/// - Adapter: provider implementation depending on contract + provider lib (outlet:adapter)
/// </summary>
public enum RegistryItemType
{
    Contract,
    Adapter,
}
