namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Command: install <paramref name="ItemName"/> (and its dependencies) into the project at
/// <paramref name="ProjectDirectory"/>. When <paramref name="Restore"/> is true (the default) the
/// affected project is restored after its packages are added, so the direct package and its
/// transitive closure are materialized and any version conflict is caught (and rolled back).
/// When <paramref name="DryRun"/> is true the install is only previewed: the report lists the files
/// and packages that WOULD be written, but nothing touches the disk, the project, or the lockfile.
/// </summary>
public sealed record AddItemCommand(string ProjectDirectory, string ItemName, bool Restore = true, bool DryRun = false);

/// <summary>
/// Summary of an install: which items were (or would be) installed, files written, any non-fatal
/// warnings, whether the affected project was restored, and whether this was a preview only.
/// </summary>
public sealed record InstallationReport(
    IReadOnlyList<string> InstalledItems,
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> Warnings,
    bool Restored = false,
    bool DryRun = false);
