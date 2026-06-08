using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>Command: update an installed item to its current registry version.</summary>
public sealed record UpdateItemCommand(string ProjectDirectory, string ItemName);

/// <summary>Summary of an update: files applied, conflicts left for manual merge, and warnings.</summary>
public sealed record UpdateReport(
    string ItemName,
    IReadOnlyList<string> Updated,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Applies the current registry version of an installed item while RESPECTING local edits:
/// untouched files are refreshed, edited-but-unchanged-upstream files are kept, and true
/// conflicts (edited locally AND changed upstream) are never clobbered — the new version is
/// written alongside as <c>&lt;file&gt;.outlet-new</c> for a manual merge. The lockfile hashes are refreshed.
/// </summary>
public sealed class UpdateItemUseCase(
    IRegistryClient registryClient,
    IProjectInspector projectInspector,
    INamespaceRewriter namespaceRewriter,
    IFileSystem fileSystem,
    INuGetEditor nuGetEditor,
    IOutletConfigStore configStore)
    : IUseCase<UpdateItemCommand, UpdateReport>
{
    private readonly ItemUpdatePlanner _planner = new(registryClient, namespaceRewriter, fileSystem);

    public async Task<Result<UpdateReport>> HandleAsync(UpdateItemCommand command, CancellationToken cancellationToken = default)
    {
        var configResult = await configStore.LoadAsync(command.ProjectDirectory, cancellationToken);
        if (configResult.IsFailure)
            return Result<UpdateReport>.Failure(configResult.Error!);
        var config = configResult.Value!;

        var planResult = await _planner.PlanAsync(config, command.ProjectDirectory, command.ItemName, cancellationToken);
        if (planResult.IsFailure)
            return Result<UpdateReport>.Failure(planResult.Error!);
        var plan = planResult.Value!;

        var installed = config.Installed.First(item => item.Name == command.ItemName);
        var hashByPath = installed.Files.ToDictionary(file => file.Path, file => file.Hash, StringComparer.Ordinal);

        var updated = new List<string>();
        var conflicts = new List<string>();
        var warnings = new List<string>();

        foreach (var change in plan.Changes)
        {
            var destinationPath = Path.Combine(command.ProjectDirectory, change.Path);
            switch (change.Status)
            {
                case FileChangeStatus.UpdateAvailable or FileChangeStatus.Missing or FileChangeStatus.New:
                    fileSystem.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? command.ProjectDirectory);
                    await fileSystem.WriteAllTextAsync(destinationPath, change.NewContent!, cancellationToken);
                    hashByPath[change.Path] = ContentHash.Of(change.NewContent!);
                    updated.Add(change.Path);
                    break;

                case FileChangeStatus.Unchanged:
                    hashByPath[change.Path] = ContentHash.Of(change.NewContent!);
                    break;

                case FileChangeStatus.Conflict:
                    await fileSystem.WriteAllTextAsync(destinationPath + ".outlet-new", change.NewContent!, cancellationToken);
                    conflicts.Add(change.Path);
                    warnings.Add(
                        $"Conflict on '{change.Path}': edited locally and changed upstream. " +
                        $"New version written to '{change.Path}.outlet-new' — merge manually.");
                    break;

                case FileChangeStatus.LocallyModified:
                    // Registry version unchanged; keep the local edits as-is.
                    break;

                case FileChangeStatus.Removed:
                    warnings.Add($"The registry no longer ships '{change.Path}'. Left in place (you own it).");
                    break;
            }
        }

        var inspection = await projectInspector.InspectAsync(command.ProjectDirectory, cancellationToken);
        foreach (var dependency in plan.Item.NugetDependencies)
        {
            var edit = await nuGetEditor.AddPackageAsync(
                new NuGetEditRequest(plan.ProjectFilePath, dependency, inspection.UsesCentralPackageManagement, inspection.CentralPackagesFilePath),
                cancellationToken);
            if (edit.Outcome == NuGetEditOutcome.Conflict && edit.Warning is not null)
                warnings.Add(edit.Warning);
        }

        IReadOnlyList<InstalledFile> refreshedFiles =
            [.. hashByPath.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new InstalledFile(pair.Key, pair.Value))];
        var updatedItem = installed with { Files = refreshedFiles };
        IReadOnlyList<InstalledItem> installedItems =
            [.. config.Installed.Select(item => item.Name == command.ItemName ? updatedItem : item)];

        await configStore.SaveAsync(command.ProjectDirectory, config with { Installed = installedItems }, cancellationToken);

        return Result<UpdateReport>.Success(new UpdateReport(command.ItemName, updated, conflicts, warnings));
    }
}
