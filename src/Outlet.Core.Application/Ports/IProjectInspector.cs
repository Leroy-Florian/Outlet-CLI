namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — preflight inspection of the user's solution/projects.
/// Implementations MUST read MSBuild-EVALUATED values
/// (dotnet msbuild -getProperty/-getItem), never the raw XML.
/// </summary>
public interface IProjectInspector
{
    Task<ProjectInspection> InspectAsync(string rootPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Evaluated facts about the target environment, gathered before any install:
/// project layout (mono vs multi-project), Central Package Management state,
/// and per-project frameworks / existing package references.
/// </summary>
public sealed record ProjectInspection(
    string WorkspaceRoot,
    bool IsMultiProject,
    IReadOnlyList<InspectedProject> Projects,
    bool UsesCentralPackageManagement,
    string? CentralPackagesFilePath);

public sealed record InspectedProject(
    string ProjectFilePath,
    IReadOnlyList<string> TargetFrameworks,
    string RootNamespace,
    IReadOnlyList<InspectedPackageReference> PackageReferences);

/// <summary>An existing PackageReference. <paramref name="Version"/> is null when governed by CPM (versionless ref).</summary>
public sealed record InspectedPackageReference(string Id, string? Version);
