using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Projects;

/// <summary>
/// SECONDARY ADAPTER — environment preflight reading MSBuild-EVALUATED values
/// via `dotnet msbuild -getProperty/-getItem`, never the raw XML.
/// Detects mono/multi-project layout, CPM + governing central file, and the
/// TFM of each project.
///
/// Bootstrap stub: implementation lands with the engine issues
/// (Linear, projet « Outlet — MVP »).
/// </summary>
public sealed class MsBuildProjectInspector : IProjectInspector
{
    public Task<ProjectInspection> InspectAsync(string rootPath, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "MSBuild preflight is not implemented yet (Linear: Outlet — MVP, engine issues).");
}
