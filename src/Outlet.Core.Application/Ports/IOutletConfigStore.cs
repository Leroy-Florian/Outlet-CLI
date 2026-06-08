using Outlet.Core.Application.Configuration;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — reads and writes the project's <c>outlet.json</c>. JSON lives
/// in the adapter; use cases see only the typed <see cref="OutletConfig"/>.
/// </summary>
public interface IOutletConfigStore
{
    bool Exists(string projectDirectory);

    /// <summary>Loads and validates the config, or returns a failed result on a malformed file.</summary>
    Task<Result<OutletConfig>> LoadAsync(string projectDirectory, CancellationToken cancellationToken = default);

    /// <summary>Writes the config to <c>outlet.json</c> (creating the directory if needed).</summary>
    Task SaveAsync(string projectDirectory, OutletConfig config, CancellationToken cancellationToken = default);
}
