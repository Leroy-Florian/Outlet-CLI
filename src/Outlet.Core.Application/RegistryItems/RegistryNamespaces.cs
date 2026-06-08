using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Naming convention for registry sources: each concern's items live under the
/// canonical root namespace <c>Outlet.Registry.&lt;Concern&gt;</c> (e.g. Outlet.Registry.Email),
/// which the namespace rewriter remaps to the user's target on install/update.
/// </summary>
public static class RegistryNamespaces
{
    public static string RootFor(ConcernName concern)
        => $"Outlet.Registry.{char.ToUpperInvariant(concern.Value[0])}{concern.Value[1..]}";
}
