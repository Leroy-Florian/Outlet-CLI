using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Infrastructure.NuGet;

/// <summary>
/// SECONDARY ADAPTER — adds direct PackageReference entries (floor versions),
/// CPM-aware: version goes to Directory.Packages.props when central management
/// is detected, PackageReference stays versionless.
///
/// Bootstrap stub: implementation lands with the engine issues
/// (Linear, projet « Outlet — MVP »).
/// </summary>
public sealed class ProjectNuGetEditor : INuGetEditor
{
    public Task AddPackageAsync(
        string projectFilePath,
        PackageDependency dependency,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "NuGet editing is not implemented yet (Linear: Outlet — MVP, engine issues).");
}
