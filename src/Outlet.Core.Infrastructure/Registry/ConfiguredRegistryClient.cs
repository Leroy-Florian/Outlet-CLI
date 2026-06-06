using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// SECONDARY ADAPTER — the <c>IRegistryClient</c> the use cases consume. It resolves
/// the configured sources (from outlet.json) per call and fans out across them via
/// <see cref="MultiSourceRegistryClient"/>, so changing registries needs no restart.
/// </summary>
public sealed class ConfiguredRegistryClient(IRegistrySourceProvider sourceProvider) : IRegistryClient
{
    public async Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default)
        => await (await AggregateAsync(cancellationToken)).GetItemsAsync(cancellationToken);

    public async Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default)
        => await (await AggregateAsync(cancellationToken)).GetItemAsync(id, cancellationToken);

    public async Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default)
        => await (await AggregateAsync(cancellationToken)).GetFileContentAsync(id, filePath, cancellationToken);

    private async Task<MultiSourceRegistryClient> AggregateAsync(CancellationToken cancellationToken)
        => new(await sourceProvider.GetSourcesAsync(cancellationToken));
}
