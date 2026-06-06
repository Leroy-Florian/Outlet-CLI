using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>Command: compare an installed item with its current registry version.</summary>
public sealed record DiffItemCommand(string ProjectDirectory, string ItemName);

/// <summary>Per-file diff status (the <see cref="FileChangeStatus"/> name).</summary>
public sealed record FileDiff(string Path, string Status);

/// <summary>Diff report for an item; <see cref="HasChanges"/> is false when everything is unchanged.</summary>
public sealed record ItemDiffReport(string ItemName, IReadOnlyList<FileDiff> Files, bool HasChanges);

/// <summary>
/// Reports, file by file, how an installed item compares with its current registry
/// version — read-only (writes nothing). The counterpart that applies is <see cref="UpdateItemUseCase"/>.
/// </summary>
public sealed class DiffItemUseCase(
    IRegistryClient registryClient,
    INamespaceRewriter namespaceRewriter,
    IFileSystem fileSystem,
    IOutletConfigStore configStore)
    : IUseCase<DiffItemCommand, ItemDiffReport>
{
    private readonly ItemUpdatePlanner _planner = new(registryClient, namespaceRewriter, fileSystem);

    public async Task<Result<ItemDiffReport>> HandleAsync(DiffItemCommand command, CancellationToken cancellationToken = default)
    {
        var config = await configStore.LoadAsync(command.ProjectDirectory, cancellationToken);
        if (config.IsFailure)
            return Result<ItemDiffReport>.Failure(config.Error!);

        var plan = await _planner.PlanAsync(config.Value!, command.ProjectDirectory, command.ItemName, cancellationToken);
        if (plan.IsFailure)
            return Result<ItemDiffReport>.Failure(plan.Error!);

        IReadOnlyList<FileDiff> files = [.. plan.Value!.Changes.Select(change => new FileDiff(change.Path, change.Status.ToString()))];
        var hasChanges = plan.Value!.Changes.Any(change => change.Status is not FileChangeStatus.Unchanged);

        return Result<ItemDiffReport>.Success(new ItemDiffReport(command.ItemName, files, hasChanges));
    }
}
