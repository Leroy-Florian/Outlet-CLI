using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Orchestrates a full <c>add</c>: resolve (HIJ-491) → fetch (HIJ-492) → rewrite
/// namespaces (HIJ-493) → write files at the routed target (HIJ-494) → add NuGet
/// packages (HIJ-495) → update the lockfile. Idempotent (already-installed items and
/// shared dependencies are skipped) and never overwrites a colliding file silently.
/// </summary>
public sealed class AddItemUseCase(
    IRegistryClient registryClient,
    IProjectInspector projectInspector,
    INamespaceRewriter namespaceRewriter,
    IFileSystem fileSystem,
    INuGetEditor nuGetEditor,
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

                var content = await registryClient.GetFileContentAsync(item.Id, file, cancellationToken);
                var rewritten = namespaceRewriter.Rewrite(content, sourceRoot, targetNamespace);

                fileSystem.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationDirectory);
                await fileSystem.WriteAllTextAsync(destinationPath, rewritten, cancellationToken);

                var relative = Path.GetRelativePath(command.ProjectDirectory, destinationPath);
                itemFiles.Add(new InstalledFile(relative, ContentHash.Of(rewritten)));
                writtenFiles.Add(relative);
            }

            var packages = new List<InstalledPackage>();
            foreach (var dependency in item.NugetDependencies)
            {
                var edit = await nuGetEditor.AddPackageAsync(
                    new NuGetEditRequest(
                        projectFilePath,
                        dependency,
                        inspection.UsesCentralPackageManagement,
                        inspection.CentralPackagesFilePath),
                    cancellationToken);

                if (edit.Outcome == NuGetEditOutcome.Conflict && edit.Warning is not null)
                    warnings.Add(edit.Warning);

                packages.Add(new InstalledPackage(dependency.PackageId, dependency.MinimumVersion));
            }

            lockEntries.Add(new InstalledItem(
                item.Id.Value,
                "0.0.0",
                itemFiles,
                packages,
                [.. item.RegistryDependencies.Select(dependency => dependency.Value)]));
            installedItems.Add(item.Id.Value);
        }

        if (lockEntries.Count > 0)
        {
            var updated = config with { Installed = [.. config.Installed, .. lockEntries] };
            await configStore.SaveAsync(command.ProjectDirectory, updated, cancellationToken);
        }

        return Result<InstallationReport>.Success(new InstallationReport(installedItems, writtenFiles, warnings));
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
