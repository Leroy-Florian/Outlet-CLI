using Outlet.Core.Application.Ports;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Infrastructure.Manifests;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// SECONDARY ADAPTER — implements <see cref="IRegistryPublisher"/> by running the
/// catalogue builder (which validates every manifest and that every declared file
/// exists) and, unless it is a dry run, writing the artifact. A malformed registry
/// fails before anything is written.
/// </summary>
public sealed class RegistryCatalogPublisher : IRegistryPublisher
{
    public Task<Result<RegistryPublicationReport>> PublishAsync(
        string registryRoot,
        string outputDirectory,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var build = RegistryCatalogBuilder.Build(registryRoot);
        if (build.IsFailure)
            return Task.FromResult(Result<RegistryPublicationReport>.Failure(build.Error!));

        var catalog = build.Value!;
        if (!dryRun)
            RegistryCatalogWriter.Write(catalog, outputDirectory);

        IReadOnlyList<PublishedRegistryItem> items =
            [.. catalog.Items.Select(item => new PublishedRegistryItem(item.Manifest.Name, item.Files.Count))];

        return Task.FromResult(Result<RegistryPublicationReport>.Success(
            new RegistryPublicationReport(outputDirectory, dryRun, items)));
    }
}
