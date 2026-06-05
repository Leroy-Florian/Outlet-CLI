namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — preflight inspection of the user's solution/projects.
/// Implementations MUST read MSBuild-EVALUATED values
/// (dotnet msbuild -getProperty/-getItem), never the raw XML.
/// </summary>
public interface IProjectInspector
{
    /// <summary>Inspects the project/solution rooted at <paramref name="rootPath"/>.</summary>
    Task<ProjectInspection> InspectAsync(string rootPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Evaluated facts about the target environment, gathered before any install:
/// project layout (mono vs multi-project), Central Package Management state,
/// and per-project target frameworks.
/// </summary>
public sealed record ProjectInspection(
    IReadOnlyList<InspectedProject> Projects,
    bool UsesCentralPackageManagement,
    string? CentralPackagesFilePath);

public sealed record InspectedProject(
    string ProjectFilePath,
    string TargetFramework,
    string RootNamespace);
