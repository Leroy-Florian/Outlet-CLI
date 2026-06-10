using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// The locked hexagonal routing rule, shared by the init preview (read-only) and the
/// init command (write): contracts land in the Application (else Domain) project,
/// adapters in Infrastructure, with a mono-project fallback to the first project.
/// </summary>
internal static class InitRouting
{
    public static (TargetRoute Contract, TargetRoute Adapter, bool Hexagonal) Resolve(
        string projectDirectory, IReadOnlyList<InspectedProject> projects)
    {
        var fallback = projects[0];

        var contractProject =
            FindBySuffix(projects, ".Application")
            ?? FindBySuffix(projects, ".Domain")
            ?? fallback;

        var adapterProject = FindBySuffix(projects, ".Infrastructure") ?? fallback;

        var contract = ToRoute(projectDirectory, contractProject);
        var adapter = ToRoute(projectDirectory, adapterProject);
        return (contract, adapter, !ReferenceEquals(contractProject, adapterProject));
    }

    public static InspectedProject? FindBySuffix(IReadOnlyList<InspectedProject> projects, string suffix) =>
        projects.FirstOrDefault(p =>
            Path.GetFileNameWithoutExtension(p.ProjectFilePath).EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    public static TargetRoute ToRoute(string projectDirectory, InspectedProject project) =>
        new(Path.GetRelativePath(projectDirectory, project.ProjectFilePath), project.RootNamespace);

    public static string RelativePath(string projectDirectory, InspectedProject project) =>
        Path.GetRelativePath(projectDirectory, project.ProjectFilePath);
}
