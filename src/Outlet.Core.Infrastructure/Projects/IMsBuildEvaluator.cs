namespace Outlet.Core.Infrastructure.Projects;

/// <summary>
/// Seam over MSBuild evaluation of one project. Isolating the process invocation
/// keeps <see cref="MsBuildProjectInspector"/> unit-testable with a hand-written fake.
/// </summary>
public interface IMsBuildEvaluator
{
    Task<MsBuildEvaluation> EvaluateAsync(string projectFilePath, CancellationToken cancellationToken = default);
}
