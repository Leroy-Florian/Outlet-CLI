using Outlet.Core.Application.Cli;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>Hand-written fake of <see cref="ICliUpdater"/>.</summary>
public sealed class FakeCliUpdater : ICliUpdater
{
    private CliUpdateOutcome _outcome = new(Succeeded: true, "updated.");

    public void SeedOutcome(bool succeeded, string detail) => _outcome = new CliUpdateOutcome(succeeded, detail);

    public Task<CliUpdateOutcome> UpdateAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_outcome);
}
