using Outlet.Core.Application.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — builds the publishable registry artifact (the aggregate index
/// plus each item's files) from a registry source tree, so a registry author can
/// produce what a remote serves over HTTP. The "manifest never lies" validation and
/// the filesystem live in the adapter; the use case sees only a typed report.
/// </summary>
public interface IRegistryPublisher
{
    Task<Result<RegistryPublicationReport>> PublishAsync(
        string registryRoot,
        string outputDirectory,
        bool dryRun,
        CancellationToken cancellationToken = default);
}
