using Outlet.Core.Infrastructure.Projects;

namespace Outlet.Core.Infrastructure.UnitTests.Fakes;

/// <summary>Hand-written <see cref="IMsBuildEvaluator"/> returning canned evaluations per project path.</summary>
public sealed class FakeMsBuildEvaluator : IMsBuildEvaluator
{
    private readonly Dictionary<string, MsBuildEvaluation> _byProject = new(StringComparer.OrdinalIgnoreCase);

    public FakeMsBuildEvaluator Map(string projectFilePath, MsBuildEvaluation evaluation)
    {
        _byProject[Path.GetFullPath(projectFilePath)] = evaluation;
        return this;
    }

    public Task<MsBuildEvaluation> EvaluateAsync(string projectFilePath, CancellationToken cancellationToken = default)
        => _byProject.TryGetValue(Path.GetFullPath(projectFilePath), out var evaluation)
            ? Task.FromResult(evaluation)
            : throw new InvalidOperationException($"No evaluation mapped for '{projectFilePath}'.");
}
