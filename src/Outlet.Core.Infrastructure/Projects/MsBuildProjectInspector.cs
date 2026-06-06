using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Projects;

/// <summary>
/// SECONDARY ADAPTER — environment preflight. Discovers the workspace projects,
/// then reads MSBuild-EVALUATED values per project (via <see cref="IMsBuildEvaluator"/>)
/// to determine mono vs multi-project layout, CPM + the governing central file,
/// per-project frameworks and existing package references. Never parses raw XML.
/// </summary>
public sealed class MsBuildProjectInspector(IMsBuildEvaluator evaluator) : IProjectInspector
{
    private const string CentralPackagesFileName = "Directory.Packages.props";

    public async Task<ProjectInspection> InspectAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var projectFiles = DiscoverProjectFiles(rootPath);

        var projects = new List<InspectedProject>();
        var usesCentralPackageManagement = false;
        string? centralPackagesFilePath = null;

        foreach (var projectFile in projectFiles)
        {
            var evaluation = await evaluator.EvaluateAsync(projectFile, cancellationToken);

            projects.Add(new InspectedProject(
                projectFile,
                ParseTargetFrameworks(evaluation.Properties),
                ResolveRootNamespace(evaluation.Properties, projectFile),
                [.. evaluation.PackageReferences.Select(ToPackageReference)]));

            if (IsTrue(evaluation.Properties.GetValueOrDefault("ManagePackageVersionsCentrally")))
            {
                usesCentralPackageManagement = true;
                centralPackagesFilePath ??= FindCentralPackagesFile(Path.GetDirectoryName(Path.GetFullPath(projectFile))!);
            }
        }

        return new ProjectInspection(
            Path.GetFullPath(rootPath),
            projects.Count > 1,
            projects,
            usesCentralPackageManagement,
            centralPackagesFilePath);
    }

    private static IReadOnlyList<string> DiscoverProjectFiles(string rootPath)
    {
        if (File.Exists(rootPath) && rootPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return [Path.GetFullPath(rootPath)];

        if (!Directory.Exists(rootPath))
            return [];

        return
        [
            .. Directory.EnumerateFiles(rootPath, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !IsUnderOutputFolder(path))
                .Select(Path.GetFullPath)
                .OrderBy(path => path, StringComparer.Ordinal),
        ];
    }

    private static bool IsUnderOutputFolder(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ParseTargetFrameworks(IReadOnlyDictionary<string, string> properties)
    {
        var multiple = properties.GetValueOrDefault("TargetFrameworks");
        if (!string.IsNullOrWhiteSpace(multiple))
            return [.. multiple.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        var single = properties.GetValueOrDefault("TargetFramework");
        return string.IsNullOrWhiteSpace(single) ? [] : [single];
    }

    private static string ResolveRootNamespace(IReadOnlyDictionary<string, string> properties, string projectFile)
    {
        var rootNamespace = properties.GetValueOrDefault("RootNamespace");
        return string.IsNullOrWhiteSpace(rootNamespace)
            ? Path.GetFileNameWithoutExtension(projectFile)
            : rootNamespace;
    }

    private static InspectedPackageReference ToPackageReference(MsBuildItem item)
    {
        var version = item.Metadata.GetValueOrDefault("Version");
        return new InspectedPackageReference(item.Identity, string.IsNullOrWhiteSpace(version) ? null : version);
    }

    private static string? FindCentralPackagesFile(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, CentralPackagesFileName);
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsTrue(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
