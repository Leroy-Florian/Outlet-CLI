using Outlet.Core.Infrastructure.Projects;
using Outlet.Core.Infrastructure.UnitTests.Fakes;

namespace Outlet.Core.Infrastructure.UnitTests.Projects;

public sealed class MsBuildProjectInspectorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "outlet-inspect-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Should_DetectMultiProjectAndCpm_When_WorkspaceHasSeveralProjectsAndCentralFile()
    {
        Write("Directory.Packages.props", "<Project />");
        var projectA = Write("src/A/A.csproj", "<Project />");
        var projectB = Write("src/B/B.csproj", "<Project />");
        Write("src/A/bin/Debug/ghost.csproj", "<Project />");

        var evaluator = new FakeMsBuildEvaluator()
            .Map(projectA, Evaluation("net10.0", cpm: true, ("Serilog", "")))
            .Map(projectB, Evaluation("net8.0;net9.0", cpm: true));

        var inspection = await new MsBuildProjectInspector(evaluator).InspectAsync(_root);

        inspection.IsMultiProject.Should().BeTrue();
        inspection.Projects.Should().HaveCount(2);
        inspection.UsesCentralPackageManagement.Should().BeTrue();
        inspection.CentralPackagesFilePath.Should().Be(Path.Combine(_root, "Directory.Packages.props"));

        var b = inspection.Projects.Single(p => p.ProjectFilePath == Path.GetFullPath(projectB));
        b.TargetFrameworks.Should().Equal("net8.0", "net9.0");

        var a = inspection.Projects.Single(p => p.ProjectFilePath == Path.GetFullPath(projectA));
        a.PackageReferences.Should().ContainSingle();
        a.PackageReferences[0].Id.Should().Be("Serilog");
        a.PackageReferences[0].Version.Should().BeNull("CPM makes the reference versionless");
    }

    [Fact]
    public async Task Should_DetectMonoProjectWithoutCpm_When_SingleProject()
    {
        var project = Write("App.csproj", "<Project />");
        var evaluator = new FakeMsBuildEvaluator()
            .Map(project, Evaluation("net10.0", cpm: false, ("MailKit", "4.16.0")));

        var inspection = await new MsBuildProjectInspector(evaluator).InspectAsync(_root);

        inspection.IsMultiProject.Should().BeFalse();
        inspection.UsesCentralPackageManagement.Should().BeFalse();
        inspection.CentralPackagesFilePath.Should().BeNull();
        inspection.Projects.Single().PackageReferences.Single().Version.Should().Be("4.16.0");
    }

    [Fact]
    public async Task Should_ExcludeBinAndObj_When_DiscoveringProjects()
    {
        var real = Write("src/Real/Real.csproj", "<Project />");
        Write("src/Real/obj/Real.csproj", "<Project />");
        var evaluator = new FakeMsBuildEvaluator().Map(real, Evaluation("net10.0", cpm: false));

        var inspection = await new MsBuildProjectInspector(evaluator).InspectAsync(_root);

        inspection.Projects.Should().ContainSingle()
            .Which.ProjectFilePath.Should().Be(Path.GetFullPath(real));
    }

    private static MsBuildEvaluation Evaluation(string targetFrameworks, bool cpm, params (string Id, string Version)[] packages)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TargetFrameworks"] = targetFrameworks.Contains(';') ? targetFrameworks : "",
            ["TargetFramework"] = targetFrameworks.Contains(';') ? "" : targetFrameworks,
            ["ManagePackageVersionsCentrally"] = cpm ? "true" : "false",
        };

        return new MsBuildEvaluation(
            properties,
            [.. packages.Select(p => new MsBuildItem(p.Id, new Dictionary<string, string> { ["Version"] = p.Version }))]);
    }

    private string Write(string relativePath, string content)
    {
        var fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
