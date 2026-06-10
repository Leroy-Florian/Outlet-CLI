using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>Query: read-only preview of what <c>init</c> would do, so the CLI can prompt before writing.</summary>
public sealed record PreviewInitProjectQuery(string ProjectDirectory);

/// <summary>A candidate destination project surfaced to the interactive prompt.</summary>
public sealed record CandidateProject(string RelativePath, string Namespace, IReadOnlyList<string> TargetFrameworks);

/// <summary>
/// What <c>init</c> would detect and propose, computed without touching disk:
/// whether a config already exists, the candidate projects, and the heuristic's
/// proposed contract/adapter targets (project-relative paths) to use as defaults.
/// </summary>
public sealed record InitProjectPreview(
    bool ConfigExists,
    bool IsMultiProject,
    bool UsesCentralPackageManagement,
    string? CentralPackagesFilePath,
    IReadOnlyList<CandidateProject> Projects,
    string? ProposedContractProject,
    string? ProposedAdapterProject);

/// <summary>
/// Drives the interactive flow: returns the detected layout and the heuristic's
/// proposed routing so the CLI can show choices and pre-select sensible defaults.
/// Writes nothing — the actual config is written by <see cref="InitProjectUseCase"/>.
/// </summary>
public sealed class PreviewInitProjectUseCase(IProjectInspector projectInspector, IOutletConfigStore configStore)
    : IUseCase<PreviewInitProjectQuery, InitProjectPreview>
{
    public async Task<Result<InitProjectPreview>> HandleAsync(PreviewInitProjectQuery query, CancellationToken cancellationToken = default)
    {
        if (configStore.Exists(query.ProjectDirectory))
            return Result<InitProjectPreview>.Success(
                new InitProjectPreview(true, false, false, null, [], null, null));

        var inspection = await projectInspector.InspectAsync(query.ProjectDirectory, cancellationToken);

        IReadOnlyList<CandidateProject> candidates =
        [
            .. inspection.Projects.Select(p => new CandidateProject(
                InitRouting.RelativePath(query.ProjectDirectory, p),
                p.RootNamespace,
                p.TargetFrameworks)),
        ];

        if (inspection.Projects.Count == 0)
            return Result<InitProjectPreview>.Success(new InitProjectPreview(
                false,
                inspection.IsMultiProject,
                inspection.UsesCentralPackageManagement,
                inspection.CentralPackagesFilePath,
                candidates,
                null,
                null));

        var (contract, adapter, _) = InitRouting.Resolve(query.ProjectDirectory, inspection.Projects);

        return Result<InitProjectPreview>.Success(new InitProjectPreview(
            false,
            inspection.IsMultiProject,
            inspection.UsesCentralPackageManagement,
            inspection.CentralPackagesFilePath,
            candidates,
            contract.Project,
            adapter.Project));
    }
}
