using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>Command: initialize <c>outlet.json</c> for the project at <paramref name="ProjectDirectory"/>.</summary>
public sealed record InitProjectCommand(string ProjectDirectory);

/// <summary>
/// Outcome of <see cref="InitProjectUseCase"/>: the written config plus the
/// environment facts it was derived from, so the CLI can show the user what was
/// detected instead of a single opaque line.
/// </summary>
public sealed record InitReport(
    OutletConfig Config,
    int ProjectCount,
    bool IsMultiProject,
    bool UsesCentralPackageManagement,
    string? CentralPackagesFilePath,
    bool HexagonalRoutingApplied);

/// <summary>
/// Creates a default <c>outlet.json</c> from the detected environment: picks the
/// destination project(s), routes both item types, and writes the config. Refuses
/// to clobber an existing config. When a hexagonal layout is detected, contracts and
/// adapters are routed to separate projects (Application/Domain vs Infrastructure).
/// </summary>
public sealed class InitProjectUseCase(IProjectInspector projectInspector, IOutletConfigStore configStore)
    : IUseCase<InitProjectCommand, InitReport>
{
    public async Task<Result<InitReport>> HandleAsync(InitProjectCommand command, CancellationToken cancellationToken = default)
    {
        if (configStore.Exists(command.ProjectDirectory))
            return Result<InitReport>.Failure($"outlet.json already exists in '{command.ProjectDirectory}'.");

        var inspection = await projectInspector.InspectAsync(command.ProjectDirectory, cancellationToken);
        if (inspection.Projects.Count == 0)
            return Result<InitReport>.Failure($"No .csproj found under '{command.ProjectDirectory}'.");

        var (contract, adapter, hexagonal) = ResolveRoutes(command.ProjectDirectory, inspection.Projects);
        var config = OutletConfig.Create(contract, adapter);

        await configStore.SaveAsync(command.ProjectDirectory, config, cancellationToken);

        var report = new InitReport(
            config,
            inspection.Projects.Count,
            inspection.IsMultiProject,
            inspection.UsesCentralPackageManagement,
            inspection.CentralPackagesFilePath,
            hexagonal);
        return Result<InitReport>.Success(report);
    }

    // Hexagonal heuristic (the locked routing rule): contracts land in the
    // Application (else Domain) project, adapters in Infrastructure. When none of
    // those projects exist we fall back to the first project for both — the
    // mono-project default.
    private static (TargetRoute Contract, TargetRoute Adapter, bool Hexagonal) ResolveRoutes(
        string projectDirectory, IReadOnlyList<InspectedProject> projects)
    {
        var fallback = projects[0];

        var contractProject =
            FindBySuffix(projects, ".Application")
            ?? FindBySuffix(projects, ".Domain")
            ?? fallback;

        var adapterProject = FindBySuffix(projects, ".Infrastructure") ?? fallback;

        var contract = ToRoute(projectDirectory, contractProject);
        var adapter = ToRoute(projectDirectory, adapterProject);
        return (contract, adapter, !ReferenceEquals(contractProject, adapterProject));
    }

    private static InspectedProject? FindBySuffix(IReadOnlyList<InspectedProject> projects, string suffix) =>
        projects.FirstOrDefault(p =>
            Path.GetFileNameWithoutExtension(p.ProjectFilePath).EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    private static TargetRoute ToRoute(string projectDirectory, InspectedProject project) =>
        new(Path.GetRelativePath(projectDirectory, project.ProjectFilePath), project.RootNamespace);
}
