using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>Command: initialize <c>outlet.json</c> for the project at <paramref name="ProjectDirectory"/>.</summary>
public sealed record InitProjectCommand(string ProjectDirectory);

/// <summary>
/// Creates a default <c>outlet.json</c> from the detected environment: picks the
/// workspace project, routes both item types to it, and writes the config. Refuses
/// to clobber an existing config.
/// </summary>
public sealed class InitProjectUseCase(IProjectInspector projectInspector, IOutletConfigStore configStore)
    : IUseCase<InitProjectCommand, OutletConfig>
{
    public async Task<Result<OutletConfig>> HandleAsync(InitProjectCommand command, CancellationToken cancellationToken = default)
    {
        if (configStore.Exists(command.ProjectDirectory))
            return Result<OutletConfig>.Failure($"outlet.json already exists in '{command.ProjectDirectory}'.");

        var inspection = await projectInspector.InspectAsync(command.ProjectDirectory, cancellationToken);
        if (inspection.Projects.Count == 0)
            return Result<OutletConfig>.Failure($"No .csproj found under '{command.ProjectDirectory}'.");

        var project = inspection.Projects[0];
        var relativeProject = Path.GetRelativePath(command.ProjectDirectory, project.ProjectFilePath);
        var config = OutletConfig.CreateDefault(relativeProject, project.RootNamespace);

        await configStore.SaveAsync(command.ProjectDirectory, config, cancellationToken);
        return Result<OutletConfig>.Success(config);
    }
}
