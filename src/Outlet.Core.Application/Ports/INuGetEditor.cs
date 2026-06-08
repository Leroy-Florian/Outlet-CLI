using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — adds a direct NuGet package reference to the user's project.
/// Rules (locked product decisions):
/// - direct dependencies only, transitives left to the resolver;
/// - versions are floors (Version="x.y.z"), never bracket locks;
/// - an existing direct reference below the floor is reported, never overwritten;
/// - under Central Package Management the version goes to Directory.Packages.props
///   and the PackageReference in the csproj stays versionless.
/// </summary>
public interface INuGetEditor
{
    Task<NuGetEditResult> AddPackageAsync(NuGetEditRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a direct package reference: the <c>PackageReference</c> from the csproj and,
    /// under CPM, the <c>PackageVersion</c> from the central file. Returns whether anything
    /// was removed. Callers decide when a package is no longer used.
    /// </summary>
    Task<bool> RemovePackageAsync(NuGetRemoveRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// What to add and the CPM context discovered by the project inspector (HIJ-510):
/// whether central management governs the target, and the central file to write to.
/// </summary>
public sealed record NuGetEditRequest(
    string ProjectFilePath,
    PackageDependency Dependency,
    bool UsesCentralPackageManagement,
    string? CentralPackagesFilePath);

public enum NuGetEditOutcome
{
    Added,
    AlreadySatisfied,
    Conflict,
}

/// <summary>Outcome of an edit; <paramref name="Warning"/> is set only on <see cref="NuGetEditOutcome.Conflict"/>.</summary>
public sealed record NuGetEditResult(NuGetEditOutcome Outcome, string? Warning = null);

/// <summary>Which package to remove from which project, with the CPM context for the central file.</summary>
public sealed record NuGetRemoveRequest(
    string ProjectFilePath,
    string PackageId,
    bool UsesCentralPackageManagement,
    string? CentralPackagesFilePath);
