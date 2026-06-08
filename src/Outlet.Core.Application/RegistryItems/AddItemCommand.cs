namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Command: install <paramref name="ItemName"/> (and its dependencies) into the project at
/// <paramref name="ProjectDirectory"/>. When <paramref name="Restore"/> is true (the default) the
/// affected project is restored after its packages are added, so the direct package and its
/// transitive closure are materialized and any version conflict is caught (and rolled back).
/// </summary>
public sealed record AddItemCommand(string ProjectDirectory, string ItemName, bool Restore = true);

/// <summary>
/// Summary of an install: which items were installed, files written, any non-fatal warnings,
/// and whether the affected project was restored.
/// </summary>
public sealed record InstallationReport(
    IReadOnlyList<string> InstalledItems,
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> Warnings,
    bool Restored = false);
