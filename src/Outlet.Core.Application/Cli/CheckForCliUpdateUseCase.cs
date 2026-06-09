using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Cli;

/// <summary>
/// Level 2 of the auto-update story: a throttled, best-effort check for a newer
/// CLI. It owns the decision "should we hit the feed at all?" (so the CLI startup
/// stays dumb) and never throws — an unreachable feed simply yields
/// <see cref="CliUpdateStatus.UpdateAvailable"/> = false with an unknown latest.
/// </summary>
public sealed class CheckForCliUpdateUseCase(
    ICliReleaseClient releaseClient,
    ICliUpdateStateStore stateStore,
    ICurrentDateTimeProvider clock)
    : IUseCase<CheckForCliUpdateQuery, CliUpdateStatus>
{
    /// <summary>How long to wait between feed lookups when the check is not forced.</summary>
    public static readonly TimeSpan ThrottleWindow = TimeSpan.FromHours(24);

    public async Task<Result<CliUpdateStatus>> HandleAsync(
        CheckForCliUpdateQuery command,
        CancellationToken cancellationToken = default)
    {
        var current = command.CurrentVersion;
        var unknown = new CliUpdateStatus(UpdateAvailable: false, current.ToString(), LatestVersion: null);

        if (!command.Force && WasCheckedRecently())
            return Result<CliUpdateStatus>.Success(unknown);

        var latest = await releaseClient.GetLatestStableVersionAsync(cancellationToken);

        // Only record the check (and thus arm the throttle) once we actually reached
        // the feed — a failed lookup should be retried on the next invocation.
        if (latest is null)
            return Result<CliUpdateStatus>.Success(unknown);

        stateStore.SaveLastCheckUtc(clock.UtcNow);

        return Result<CliUpdateStatus>.Success(new CliUpdateStatus(
            UpdateAvailable: latest.IsNewerThan(current),
            current.ToString(),
            latest.ToString()));
    }

    private bool WasCheckedRecently()
    {
        var lastCheck = stateStore.ReadLastCheckUtc();
        return lastCheck is not null && clock.UtcNow - lastCheck.Value < ThrottleWindow;
    }
}
