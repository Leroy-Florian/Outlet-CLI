using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Removes an installed item: deletes its files, cleans up any PackageReference no longer
/// used by another installed item, updates the lockfile, and warns when other installed
/// items still depend on it. The symmetric counterpart of <see cref="AddItemUseCase"/>.
/// </summary>
public sealed class RemoveItemUseCase(
    IProjectInspector projectInspector,
    IFileSystem fileSystem,
    INuGetEditor nuGetEditor,
    IOutletConfigStore configStore)
    : IUseCase<RemoveItemCommand, RemovalReport>
{
    public async Task<Result<RemovalReport>> HandleAsync(RemoveItemCommand command, CancellationToken cancellationToken = default)
    {
        var configResult = await configStore.LoadAsync(command.ProjectDirectory, cancellationToken);
        if (configResult.IsFailure)
            return Result<RemovalReport>.Failure(configResult.Error!);
        var config = configResult.Value!;

        var target = config.Installed.FirstOrDefault(item => item.Name == command.ItemName);
        if (target is null)
            return Result<RemovalReport>.Failure($"Item '{command.ItemName}' is not installed.");

        List<InstalledItem> remaining = [.. config.Installed.Where(item => item.Name != command.ItemName)];
        var inspection = await projectInspector.InspectAsync(command.ProjectDirectory, cancellationToken);

        var warnings = new List<string>();
        List<string> dependents = [.. remaining.Where(item => item.Dependencies.Contains(command.ItemName)).Select(item => item.Name)];
        if (dependents.Count > 0)
            warnings.Add($"'{command.ItemName}' is still used by: {string.Join(", ", dependents)}. Their code may no longer compile.");

        var deletedFiles = new List<string>();
        foreach (var file in target.Files)
        {
            var path = Path.Combine(command.ProjectDirectory, file.Path);
            if (fileSystem.FileExists(path))
            {
                fileSystem.DeleteFile(path);
                deletedFiles.Add(file.Path);
            }
        }

        // Only clean a package the removed item brought in if no remaining item still needs it.
        var stillUsed = remaining.SelectMany(item => item.Packages).Select(package => package.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var adapterProject = Path.Combine(command.ProjectDirectory, config.Targets.Adapter.Project);
        var removedPackages = new List<string>();
        foreach (var package in target.Packages)
        {
            if (stillUsed.Contains(package.Id))
                continue;

            var removed = await nuGetEditor.RemovePackageAsync(
                new NuGetRemoveRequest(
                    adapterProject,
                    package.Id,
                    inspection.UsesCentralPackageManagement,
                    inspection.CentralPackagesFilePath),
                cancellationToken);

            if (removed)
                removedPackages.Add(package.Id);
        }

        await configStore.SaveAsync(command.ProjectDirectory, config with { Installed = remaining }, cancellationToken);

        return Result<RemovalReport>.Success(new RemovalReport(command.ItemName, deletedFiles, removedPackages, warnings));
    }
}
