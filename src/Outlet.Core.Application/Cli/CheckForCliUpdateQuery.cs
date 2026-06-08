using Outlet.Core.Domain.Cli;

namespace Outlet.Core.Application.Cli;

/// <summary>
/// Query: is a newer Outlet CLI available than <paramref name="CurrentVersion"/>?
/// When <paramref name="Force"/> is false the check is throttled (skipped if the
/// feed was queried recently); set it to true to bypass the throttle.
/// </summary>
public sealed record CheckForCliUpdateQuery(CliVersion CurrentVersion, bool Force = false);
