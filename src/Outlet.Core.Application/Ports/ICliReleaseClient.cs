using Outlet.Core.Domain.Cli;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — discovers the latest published version of the Outlet CLI
/// (from NuGet, or any configured package feed).
///
/// Best-effort by contract: when the feed is unreachable or returns nothing
/// usable, implementations return <c>null</c> ("unknown") rather than throwing,
/// so the background update check never breaks an offline invocation.
/// </summary>
public interface ICliReleaseClient
{
    /// <summary>The latest STABLE version available, or null when it cannot be determined.</summary>
    Task<CliVersion?> GetLatestStableVersionAsync(CancellationToken cancellationToken = default);
}
