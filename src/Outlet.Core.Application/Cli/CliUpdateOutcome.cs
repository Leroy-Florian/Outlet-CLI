namespace Outlet.Core.Application.Cli;

/// <summary>
/// Result of running the self-update: whether the underlying tool succeeded,
/// plus the human-readable detail (tool output or error) to surface to the user.
/// </summary>
public sealed record CliUpdateOutcome(bool Succeeded, string Detail);
