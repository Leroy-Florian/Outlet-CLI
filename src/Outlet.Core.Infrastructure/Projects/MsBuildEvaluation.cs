namespace Outlet.Core.Infrastructure.Projects;

/// <summary>Evaluated MSBuild facts for a single project: requested properties + PackageReference items.</summary>
public sealed record MsBuildEvaluation(
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyList<MsBuildItem> PackageReferences);

/// <summary>An evaluated MSBuild item: its <paramref name="Identity"/> plus metadata (e.g. Version).</summary>
public sealed record MsBuildItem(string Identity, IReadOnlyDictionary<string, string> Metadata);
