using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.Cli;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>
/// Hand-written fake of <see cref="ICliReleaseClient"/>. Null latest models an
/// unreachable feed (the best-effort "unknown" outcome).
/// </summary>
public sealed class FakeCliReleaseClient : ICliReleaseClient
{
    private CliVersion? _latest;

    public int Calls { get; private set; }

    public void SeedLatest(string version) => _latest = CliVersion.From(version);

    public void SeedUnreachable() => _latest = null;

    public Task<CliVersion?> GetLatestStableVersionAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(_latest);
    }
}
