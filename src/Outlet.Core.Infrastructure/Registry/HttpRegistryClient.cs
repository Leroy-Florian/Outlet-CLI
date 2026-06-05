using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// SECONDARY ADAPTER — fetches *.registry.json manifests and item files
/// served over HTTP (dist/registry/), multi-source by design.
///
/// Bootstrap stub: no registry source is configured yet, so the catalogue is
/// empty. Remote fetch + manifest deserialization land with the engine issues
/// (Linear, projet « Outlet — MVP »).
/// </summary>
public sealed class HttpRegistryClient(HttpClient httpClient) : IRegistryClient
{
    // Keeps the dependency visible (and the DI wiring honest) until remote fetch lands.
    private readonly HttpClient _httpClient = httpClient;

    public Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        _ = _httpClient;
        return Task.FromResult<IReadOnlyList<RegistryItem>>([]);
    }

    public Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default)
        => Task.FromResult<RegistryItem?>(null);

    public Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "Remote file fetch is not implemented yet (Linear: Outlet — MVP, engine issues).");
}
