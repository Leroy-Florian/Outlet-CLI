using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — adds NuGet package references to the user's project.
/// Rules (locked product decisions):
/// - direct dependencies only, transitives left to the resolver;
/// - versions are floors, never locks;
/// - on direct conflict, warn instead of overwriting;
/// - when Central Package Management is detected, the version goes to
///   Directory.Packages.props and the PackageReference stays versionless.
/// </summary>
public interface INuGetEditor
{
    Task AddPackageAsync(
        string projectFilePath,
        PackageDependency dependency,
        CancellationToken cancellationToken = default);
}
