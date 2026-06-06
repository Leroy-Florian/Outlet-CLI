using Outlet.Kernel.Shared;

namespace Outlet.Core.Domain.RegistryItems;

/// <summary>
/// AGGREGATE ROOT — a single installable item of the Outlet registry
/// (e.g. "email-abstractions" contract, "email-smtp" adapter).
///
/// Invariants enforced here:
/// - An item always belongs to exactly one concern.
/// - A CONTRACT item never carries NuGet dependencies (zero external dependency rule).
/// - An item always ships at least one file (the manifest must never lie).
/// </summary>
public sealed class RegistryItem : AggregateRoot<RegistryItemId>
{
    public ConcernName Concern { get; }
    public RegistryItemType Type { get; }

    private readonly List<string> _files;
    public IReadOnlyList<string> Files => _files;

    private readonly List<RegistryItemId> _registryDependencies;
    public IReadOnlyList<RegistryItemId> RegistryDependencies => _registryDependencies;

    private readonly List<PackageDependency> _nugetDependencies;
    public IReadOnlyList<PackageDependency> NugetDependencies => _nugetDependencies;

    private readonly List<string> _targetFrameworks;

    /// <summary>Target frameworks the item is known to build against (e.g. "net8.0"); used for the install pre-check.</summary>
    public IReadOnlyList<string> TargetFrameworks => _targetFrameworks;

    private RegistryItem(
        RegistryItemId id,
        ConcernName concern,
        RegistryItemType type,
        IEnumerable<string> files,
        IEnumerable<RegistryItemId> registryDependencies,
        IEnumerable<PackageDependency> nugetDependencies,
        IEnumerable<string> targetFrameworks)
        : base(id)
    {
        Concern = concern;
        Type = type;
        _files = [.. files];
        _registryDependencies = [.. registryDependencies];
        _nugetDependencies = [.. nugetDependencies];
        _targetFrameworks = [.. targetFrameworks];
    }

    public static Result<RegistryItem> Create(
        RegistryItemId id,
        ConcernName concern,
        RegistryItemType type,
        IReadOnlyCollection<string> files,
        IReadOnlyCollection<RegistryItemId>? registryDependencies = null,
        IReadOnlyCollection<PackageDependency>? nugetDependencies = null,
        IReadOnlyCollection<string>? targetFrameworks = null)
    {
        if (files.Count == 0)
            return Result<RegistryItem>.Failure($"Registry item '{id}' must ship at least one file.");

        if (type == RegistryItemType.Contract && nugetDependencies is { Count: > 0 })
            return Result<RegistryItem>.Failure(
                $"Contract item '{id}' must have zero external dependencies, " +
                $"but declares {nugetDependencies.Count} NuGet package(s).");

        return Result<RegistryItem>.Success(new RegistryItem(
            id,
            concern,
            type,
            files,
            registryDependencies ?? [],
            nugetDependencies ?? [],
            targetFrameworks ?? []));
    }
}
