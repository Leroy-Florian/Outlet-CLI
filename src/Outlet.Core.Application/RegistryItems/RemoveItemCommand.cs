namespace Outlet.Core.Application.RegistryItems;

/// <summary>Command: remove an installed item from the project at <paramref name="ProjectDirectory"/>.</summary>
public sealed record RemoveItemCommand(string ProjectDirectory, string ItemName);

/// <summary>Summary of a removal: files deleted, packages cleaned, and any warnings.</summary>
public sealed record RemovalReport(
    string RemovedItem,
    IReadOnlyList<string> DeletedFiles,
    IReadOnlyList<string> RemovedPackages,
    IReadOnlyList<string> Warnings);
