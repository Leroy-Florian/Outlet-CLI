namespace Outlet.Core.Application.Cli;

/// <summary>
/// Outcome of an update check. <see cref="LatestVersion"/> is null when the check
/// was throttled or the feed could not be reached (latest version unknown).
/// </summary>
public sealed record CliUpdateStatus(bool UpdateAvailable, string CurrentVersion, string? LatestVersion);
