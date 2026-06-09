using Outlet.Core.Application.Cli;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — performs the actual self-update of the Outlet CLI
/// (e.g. by shelling out to <c>dotnet tool update --global</c>).
/// </summary>
public interface ICliUpdater
{
    Task<CliUpdateOutcome> UpdateAsync(CancellationToken cancellationToken = default);
}
