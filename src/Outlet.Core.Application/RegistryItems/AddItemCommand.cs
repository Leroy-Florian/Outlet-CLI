namespace Outlet.Core.Application.RegistryItems;

/// <summary>Command: install <paramref name="ItemName"/> (and its dependencies) into the project at <paramref name="ProjectDirectory"/>.</summary>
public sealed record AddItemCommand(string ProjectDirectory, string ItemName);

/// <summary>Summary of an install: which items were installed, files written, and any non-fatal warnings.</summary>
public sealed record InstallationReport(
    IReadOnlyList<string> InstalledItems,
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> Warnings);
