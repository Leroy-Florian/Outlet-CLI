namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Decides whether a registry item (which ships copied source) is safe to install into
/// a project, by comparing the item's declared <c>targetFrameworks</c> with the project's.
///
/// Rule: the copied source must compile under <b>every</b> framework the target project
/// builds. A project framework is supported when the item lists it exactly, or when it is
/// a .NET (net5.0+) framework at or above the item's lowest declared .NET framework
/// (forward compatibility of source).
/// </summary>
public static class TargetFrameworkCompatibility
{
    /// <summary>Returns null when compatible, otherwise a human-readable reason to refuse.</summary>
    public static string? Check(
        string itemName,
        IReadOnlyList<string> itemFrameworks,
        IReadOnlyList<string> projectFrameworks)
    {
        // Nothing declared on either side → cannot meaningfully verify, do not block.
        if (itemFrameworks.Count == 0 || projectFrameworks.Count == 0)
            return null;

        var itemMinimum = itemFrameworks
            .Select(ParseDotNet)
            .Where(version => version is not null)
            .Select(version => version!)
            .DefaultIfEmpty()
            .Min();

        var unsupported = projectFrameworks
            .Where(projectFramework => !IsSupported(projectFramework, itemFrameworks, itemMinimum))
            .ToList();

        if (unsupported.Count == 0)
            return null;

        return $"Item '{itemName}' supports [{string.Join(", ", itemFrameworks)}] " +
               $"but the target project targets [{string.Join(", ", projectFrameworks)}] " +
               $"(incompatible: {string.Join(", ", unsupported)}).";
    }

    private static bool IsSupported(string projectFramework, IReadOnlyList<string> itemFrameworks, Version? itemMinimum)
    {
        if (itemFrameworks.Contains(projectFramework, StringComparer.OrdinalIgnoreCase))
            return true;

        var projectVersion = ParseDotNet(projectFramework);
        return projectVersion is not null && itemMinimum is not null && projectVersion >= itemMinimum;
    }

    // Parses a modern .NET TFM ("net8.0", "net10.0") into a Version; null for anything else
    // (netstandard2.0, net48, net8.0-windows, …) so only exact matches accept those.
    private static Version? ParseDotNet(string framework)
    {
        if (!framework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = framework[3..];
        return Version.TryParse(rest, out var version) && rest.Contains('.') ? version : null;
    }
}
