using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Command: initialize <c>outlet.json</c> for the project at <paramref name="ProjectDirectory"/>.
/// When <paramref name="ContractProject"/>/<paramref name="AdapterProject"/> are supplied (the
/// interactive flow), they override the auto-routing — each is a project-relative path as listed
/// by <see cref="PreviewInitProjectQuery"/>. When null, the hexagonal heuristic decides that side.
/// </summary>
public sealed record InitProjectCommand(
    string ProjectDirectory,
    string? ContractProject = null,
    string? AdapterProject = null);

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
    private sealed record RouteResolution(TargetRoute Contract, TargetRoute Adapter, bool Hexagonal);

    public async Task<Result<InitReport>> HandleAsync(InitProjectCommand command, CancellationToken cancellationToken = default)
    {
        if (configStore.Exists(command.ProjectDirectory))
            return Result<InitReport>.Failure($"outlet.json already exists in '{command.ProjectDirectory}'.");

        var inspection = await projectInspector.InspectAsync(command.ProjectDirectory, cancellationToken);
        if (inspection.Projects.Count == 0)
            return Result<InitReport>.Failure($"No .csproj found under '{command.ProjectDirectory}'.");

        var routes = ResolveRoutes(command, inspection.Projects);
        if (routes.IsFailure)
            return Result<InitReport>.Failure(routes.Error!);

        var resolution = routes.Value!;
        var config = OutletConfig.Create(resolution.Contract, resolution.Adapter);

        await configStore.SaveAsync(command.ProjectDirectory, config, cancellationToken);

        var report = new InitReport(
            config,
            inspection.Projects.Count,
            inspection.IsMultiProject,
            inspection.UsesCentralPackageManagement,
            inspection.CentralPackagesFilePath,
            resolution.Hexagonal);
        return Result<InitReport>.Success(report);
    }

    private static Result<RouteResolution> ResolveRoutes(
        InitProjectCommand command, IReadOnlyList<InspectedProject> projects)
    {
        var (heuristicContract, heuristicAdapter, _) = InitRouting.Resolve(command.ProjectDirectory, projects);

        var contract = heuristicContract;
        if (command.ContractProject is not null)
        {
            var picked = Pick(command.ProjectDirectory, projects, command.ContractProject);
            if (picked is null)
                return Result<RouteResolution>.Failure($"Unknown project '{command.ContractProject}'.");
            contract = InitRouting.ToRoute(command.ProjectDirectory, picked);
        }

        var adapter = heuristicAdapter;
        if (command.AdapterProject is not null)
        {
            var picked = Pick(command.ProjectDirectory, projects, command.AdapterProject);
            if (picked is null)
                return Result<RouteResolution>.Failure($"Unknown project '{command.AdapterProject}'.");
            adapter = InitRouting.ToRoute(command.ProjectDirectory, picked);
        }

        var hexagonal = !string.Equals(contract.Project, adapter.Project, StringComparison.OrdinalIgnoreCase);
        return Result<RouteResolution>.Success(new RouteResolution(contract, adapter, hexagonal));
    }

    private static InspectedProject? Pick(string projectDirectory, IReadOnlyList<InspectedProject> projects, string relativePath) =>
        projects.FirstOrDefault(p =>
            string.Equals(InitRouting.RelativePath(projectDirectory, p), relativePath, StringComparison.OrdinalIgnoreCase));
}
