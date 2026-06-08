using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>How an installed file compares against the current registry version and the local copy.</summary>
public enum FileChangeStatus
{
    /// <summary>Local copy already equals the new registry version.</summary>
    Unchanged,

    /// <summary>Local copy is the original (unedited) and the registry has a newer version → safe to apply.</summary>
    UpdateAvailable,

    /// <summary>Local copy was edited; the registry version is unchanged → keep the local edits.</summary>
    LocallyModified,

    /// <summary>Both the local copy and the registry changed → cannot apply safely.</summary>
    Conflict,

    /// <summary>The file is recorded but missing on disk → can be restored.</summary>
    Missing,

    /// <summary>The registry version ships a file not yet installed → can be added.</summary>
    New,

    /// <summary>The lockfile records a file the registry no longer ships.</summary>
    Removed,
}

/// <summary>A planned change for one file; <see cref="NewContent"/> is null for <see cref="FileChangeStatus.Removed"/>.</summary>
public sealed record FileChange(string Path, FileChangeStatus Status, string? NewContent);

/// <summary>The result of comparing an installed item with its current registry version.</summary>
public sealed record ItemUpdatePlan(string ItemName, RegistryItem Item, string ProjectFilePath, IReadOnlyList<FileChange> Changes);

/// <summary>
/// Compares an installed item against its current registry version, file by file, using the
/// hashes recorded at install time to tell untouched copies from local edits. Shared by
/// <c>diff</c> (HIJ-500, report only) and <c>update</c> (apply).
/// </summary>
public sealed class ItemUpdatePlanner(IRegistryClient registryClient, INamespaceRewriter namespaceRewriter, IFileSystem fileSystem)
{
    public async Task<Result<ItemUpdatePlan>> PlanAsync(
        OutletConfig config,
        string projectDirectory,
        string itemName,
        CancellationToken cancellationToken = default)
    {
        var installed = config.Installed.FirstOrDefault(item => item.Name == itemName);
        if (installed is null)
            return Result<ItemUpdatePlan>.Failure($"Item '{itemName}' is not installed.");

        RegistryItemId id;
        try
        {
            id = RegistryItemId.From(itemName);
        }
        catch (ArgumentException ex)
        {
            return Result<ItemUpdatePlan>.Failure($"Invalid item name '{itemName}': {ex.Message}");
        }

        var item = await registryClient.GetItemAsync(id, cancellationToken);
        if (item is null)
            return Result<ItemUpdatePlan>.Failure($"Item '{itemName}' was not found in any configured registry.");

        var route = item.Type == RegistryItemType.Contract ? config.Targets.Contract : config.Targets.Adapter;
        var projectFilePath = Path.Combine(projectDirectory, route.Project);
        var destinationDirectory = Path.GetDirectoryName(projectFilePath) ?? projectDirectory;
        var sourceRoot = TargetNamespace.From(RegistryNamespaces.RootFor(item.Concern));
        var targetNamespace = TargetNamespace.From(route.Namespace);

        var baseHashByPath = installed.Files.ToDictionary(file => file.Path, file => file.Hash, StringComparer.Ordinal);
        var registryPaths = new HashSet<string>(StringComparer.Ordinal);
        var changes = new List<FileChange>();

        foreach (var file in item.Files)
        {
            var destinationPath = Path.Combine(destinationDirectory, file);
            var relative = Path.GetRelativePath(projectDirectory, destinationPath);
            registryPaths.Add(relative);

            var fetched = await registryClient.GetFileContentAsync(id, file, cancellationToken);
            var newContent = namespaceRewriter.Rewrite(fetched, sourceRoot, targetNamespace);

            var localContent = fileSystem.FileExists(destinationPath)
                ? await fileSystem.ReadAllTextAsync(destinationPath, cancellationToken)
                : null;

            var status = Classify(localContent, newContent, baseHashByPath.GetValueOrDefault(relative));
            changes.Add(new FileChange(relative, status, newContent));
        }

        foreach (var file in installed.Files)
        {
            if (!registryPaths.Contains(file.Path))
                changes.Add(new FileChange(file.Path, FileChangeStatus.Removed, null));
        }

        return Result<ItemUpdatePlan>.Success(new ItemUpdatePlan(itemName, item, projectFilePath, changes));
    }

    private static FileChangeStatus Classify(string? localContent, string newContent, string? baseHash)
    {
        if (localContent is null)
            return baseHash is null ? FileChangeStatus.New : FileChangeStatus.Missing;

        var localHash = ContentHash.Of(localContent);
        var newHash = ContentHash.Of(newContent);

        if (localHash == newHash)
            return FileChangeStatus.Unchanged;
        if (baseHash is null)
            return FileChangeStatus.Conflict;
        if (localHash == baseHash)
            return FileChangeStatus.UpdateAvailable;
        if (newHash == baseHash)
            return FileChangeStatus.LocallyModified;
        return FileChangeStatus.Conflict;
    }
}
