namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — restores a project's NuGet graph after package references were
/// added, materializing the direct package AND its transitive closure, and surfacing
/// the version-conflict diagnostics NuGet emits. Implementations shell out to
/// <c>dotnet restore</c>; use cases stay hermetic via a hand-written fake.
/// </summary>
public interface IPackageRestorer
{
    Task<RestoreResult> RestoreAsync(RestoreRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Which project to restore.</summary>
public sealed record RestoreRequest(string ProjectFilePath);

/// <summary>
/// Outcome of a restore. <paramref name="Succeeded"/> is false when NuGet could not
/// resolve a coherent graph (a real version conflict). <paramref name="Diagnostics"/>
/// carries the NUxxxx messages (the errors that explain a failure, or downgrade/compat
/// warnings on an otherwise successful restore) so the CLI can show them to the user.
/// </summary>
public sealed record RestoreResult(bool Succeeded, IReadOnlyList<string> Diagnostics);
