using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Orchestrates a full <c>add</c>: resolve (HIJ-491) → fetch (HIJ-492) → rewrite
/// namespaces (HIJ-493) → write files at the routed target (HIJ-494) → add NuGet
/// packages (HIJ-495) → restore the affected project so the direct package and its
/// transitive closure are materialized and any version conflict is caught → update
/// the lockfile. Idempotent (already-installed items and shared dependencies are
/// skipped) and never overwrites a colliding file silently. If the restore reveals an
/// incompatible package graph the whole install is rolled back atomically.
/// </summary>
public sealed class AddItemUseCase(
    IRegistryClient registryClient,
    IProjectInspector projectInspector,
    INamespaceRewriter namespaceRewriter,
    IFileSystem fileSystem,
    INuGetEditor nuGetEditor,
    IPackageRestorer packageRestorer,
    IOutletConfigStore configStore)
    : IUseCase<AddItemCommand, InstallationReport>
{
    private readonly RegistryDependencyResolver _resolver = new(registryClient);

    public async Task<Result<InstallationReport>> HandleAsync(AddItemCommand command, CancellationToken cancellationToken = default)
    {
        var configResult = await configStore.LoadAsync(command.ProjectDirectory, cancellationToken);
        if (configResult.IsFailure)
            return Result<InstallationReport>.Failure(configResult.Error!);
        var config = configResult.Value!;

        var resolution = await _resolver.ResolveAsync(command.ItemName, cancellationToken);
        if (resolution.IsFailure)
            return Result<InstallationReport>.Failure(resolution.Error!);

        var inspection = await projectInspector.InspectAsync(command.ProjectDirectory, cancellationToken);

        var alreadyInstalled = config.Installed.Select(i => i.Name).ToHashSet(StringComparer.Ordinal);

        // Pre-check TFM compatibility for the whole install set BEFORE writing anything,
        // so an incompatible item is refused cleanly instead of half-installed.
        var incompatibility = CheckCompatibility(resolution.Value!, alreadyInstalled, config, command.ProjectDirectory, inspection);
        if (incompatibility is not null)
            return Result<InstallationReport>.Failure(incompatibility);

        var installedItems = new List<string>();
        var writtenFiles = new List<string>();
        var warnings = new List<string>();
        var lockEntries = new List<InstalledItem>();

        // For an atomic rollback if the restore later fails: the absolute paths we wrote,
        // and the references WE added (outcome Added) — never the pre-existing ones.
        var writtenAbsolutePaths = new List<string>();
        var addedReferences = new List<NuGetEditRequest>();

        foreach (var item in resolution.Value!)
        {
            if (alreadyInstalled.Contains(item.Id.Value))
                continue;

            var route = item.Type == RegistryItemType.Contract ? config.Targets.Contract : config.Targets.Adapter;
            var projectFilePath = Path.Combine(command.ProjectDirectory, route.Project);
            var destinationDirectory = Path.GetDirectoryName(projectFilePath) ?? command.ProjectDirectory;
            var sourceRoot = TargetNamespace.From(RegistryNamespaces.RootFor(item.Concern));
            var targetNamespace = TargetNamespace.From(route.Namespace);

            var itemFiles = new List<InstalledFile>();
            foreach (var file in item.Files)
            {
                var destinationPath = Path.Combine(destinationDirectory, file);
                if (fileSystem.FileExists(destinationPath))
                {
                    warnings.Add($"Skipped '{file}' for item '{item.Id.Value}': '{destinationPath}' already exists.");
                    continue;
                }

                var relative = Path.GetRelativePath(command.ProjectDirectory, destinationPath);
                writtenFiles.Add(relative);

                // Preview only: record what WOULD be written, but never fetch/rewrite/touch the disk.
                if (command.DryRun)
                    continue;

                var content = await registryClient.GetFileContentAsync(item.Id, file, cancellationToken);
                var rewritten = namespaceRewriter.Rewrite(content, sourceRoot, targetNamespace);

                fileSystem.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationDirectory);
                await fileSystem.WriteAllTextAsync(destinationPath, rewritten, cancellationToken);

                itemFiles.Add(new InstalledFile(relative, ContentHash.Of(rewritten)));
                writtenAbsolutePaths.Add(destinationPath);
            }

            var packages = new List<InstalledPackage>();
            foreach (var dependency in item.NugetDependencies)
            {
                packages.Add(new InstalledPackage(dependency.PackageId, dependency.MinimumVersion));

                // Preview only: record the package, but never edit the project file.
                if (command.DryRun)
                    continue;

                var request = new NuGetEditRequest(
                    projectFilePath,
                    dependency,
                    inspection.UsesCentralPackageManagement,
                    inspection.CentralPackagesFilePath);

                var edit = await nuGetEditor.AddPackageAsync(request, cancellationToken);

                if (edit.Outcome == NuGetEditOutcome.Conflict && edit.Warning is not null)
                    warnings.Add(edit.Warning);

                if (edit.Outcome == NuGetEditOutcome.Added)
                    addedReferences.Add(request);
            }

            lockEntries.Add(new InstalledItem(
                item.Id.Value,
                "0.0.0",
                itemFiles,
                packages,
                [.. item.RegistryDependencies.Select(dependency => dependency.Value)]));
            installedItems.Add(item.Id.Value);
        }

        // Restore the affected projects so the direct packages and their transitive
        // closure are materialized — and a version conflict surfaces now, not at build.
        // On conflict the whole install is rolled back and nothing is committed.
        var restored = false;
        if (command.Restore && addedReferences.Count > 0)
        {
            var restoreError = await RestoreOrRollbackAsync(
                command.ItemName, addedReferences, writtenAbsolutePaths, warnings, cancellationToken);
            if (restoreError is not null)
                return Result<InstallationReport>.Failure(restoreError);

            restored = true;
        }

        // A preview never persists the lockfile — the project must be left exactly as it was found.
        if (!command.DryRun && lockEntries.Count > 0)
        {
            var updated = config with { Installed = [.. config.Installed, .. lockEntries] };
            await configStore.SaveAsync(command.ProjectDirectory, updated, cancellationToken);
        }

        return Result<InstallationReport>.Success(
            new InstallationReport(installedItems, writtenFiles, warnings, restored, command.DryRun));
    }

    /// <summary>
    /// Restores every project whose graph changed. Returns null on success (restore
    /// warnings are appended to <paramref name="warnings"/>); on the first failed
    /// restore it rolls the install back and returns an error message.
    /// </summary>
    private async Task<string?> RestoreOrRollbackAsync(
        string itemName,
        IReadOnlyList<NuGetEditRequest> addedReferences,
        IReadOnlyList<string> writtenAbsolutePaths,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var projects = (IReadOnlyList<string>)[.. addedReferences
            .Select(reference => reference.ProjectFilePath)
            .Distinct(StringComparer.Ordinal)];

        var diagnostics = new List<string>();
        foreach (var project in projects)
        {
            var restore = await packageRestorer.RestoreAsync(new RestoreRequest(project), cancellationToken);
            diagnostics.AddRange(restore.Diagnostics);

            if (!restore.Succeeded)
            {
                await RollbackAsync(addedReferences, writtenAbsolutePaths, cancellationToken);

                var detail = restore.Diagnostics.Count > 0
                    ? string.Join("; ", restore.Diagnostics)
                    : "see 'dotnet restore' output";
                return $"'{itemName}': NuGet could not resolve a compatible package graph ({detail}). " +
                    "Rolled back the install — your project was left unchanged. " +
                    "Resolve the version conflict (align the conflicting package's version in your project, " +
                    "or remove the existing reference), then run 'outlet add' again — " +
                    "or pass '--no-restore' to copy the files without restoring.";
            }
        }

        warnings.AddRange(diagnostics);
        return null;
    }

    /// <summary>
    /// Undoes a failed install: deletes the files we wrote and removes the references we
    /// added (never the pre-existing ones). The lockfile is only saved after a clean
    /// restore, so there is nothing to revert there.
    /// </summary>
    private async Task RollbackAsync(
        IReadOnlyList<NuGetEditRequest> addedReferences,
        IReadOnlyList<string> writtenAbsolutePaths,
        CancellationToken cancellationToken)
    {
        foreach (var path in writtenAbsolutePaths)
            fileSystem.DeleteFile(path);

        foreach (var reference in addedReferences)
            await nuGetEditor.RemovePackageAsync(
                new NuGetRemoveRequest(
                    reference.ProjectFilePath,
                    reference.Dependency.PackageId,
                    reference.UsesCentralPackageManagement,
                    reference.CentralPackagesFilePath),
                cancellationToken);
    }

    private static string? CheckCompatibility(
        IReadOnlyList<RegistryItem> items,
        HashSet<string> alreadyInstalled,
        OutletConfig config,
        string projectDirectory,
        ProjectInspection inspection)
    {
        var projectsByPath = new Dictionary<string, InspectedProject>(StringComparer.Ordinal);
        foreach (var project in inspection.Projects)
            projectsByPath[Path.GetFullPath(project.ProjectFilePath)] = project;

        foreach (var item in items)
        {
            if (alreadyInstalled.Contains(item.Id.Value))
                continue;

            var route = item.Type == RegistryItemType.Contract ? config.Targets.Contract : config.Targets.Adapter;
            var projectFilePath = Path.GetFullPath(Path.Combine(projectDirectory, route.Project));
            if (!projectsByPath.TryGetValue(projectFilePath, out var project))
                continue; // target project not discovered — cannot verify, do not block

            var reason = TargetFrameworkCompatibility.Check(item.Id.Value, item.TargetFrameworks, project.TargetFrameworks);
            if (reason is not null)
                return reason;
        }

        return null;
    }
}
