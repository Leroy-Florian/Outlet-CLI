using Outlet.Core.Application.Ports;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>Hand-written <see cref="IProjectInspector"/> returning a canned inspection.</summary>
public sealed class FakeProjectInspector : IProjectInspector
{
    private ProjectInspection _inspection = new("/repo", false, [], false, null);

    public FakeProjectInspector WithCentralPackageManagement(string centralFilePath)
    {
        _inspection = _inspection with { UsesCentralPackageManagement = true, CentralPackagesFilePath = centralFilePath };
        return this;
    }

    public FakeProjectInspector WithProject(string projectFilePath, string rootNamespace, params string[] targetFrameworks)
    {
        var project = new InspectedProject(projectFilePath, [.. targetFrameworks], rootNamespace, []);
        var projects = (IReadOnlyList<InspectedProject>)[.. _inspection.Projects, project];
        _inspection = _inspection with { Projects = projects, IsMultiProject = projects.Count > 1 };
        return this;
    }

    public Task<ProjectInspection> InspectAsync(string rootPath, CancellationToken cancellationToken = default)
        => Task.FromResult(_inspection);
}
