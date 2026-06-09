namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — persists the timestamp of the last update check so the
/// background check can be throttled (we only hit the feed once per window).
///
/// State lives outside any project (per-user), independent of the working
/// directory. Best-effort: read returns null when no state exists or it cannot
/// be read; save never throws.
/// </summary>
public interface ICliUpdateStateStore
{
    /// <summary>The UTC instant of the last successful check, or null if never checked.</summary>
    DateTime? ReadLastCheckUtc();

    /// <summary>Records that a check happened at the given UTC instant.</summary>
    void SaveLastCheckUtc(DateTime timestampUtc);
}
