namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// Yields the registry sources for the current project by reading its
/// <c>outlet.json</c> registries. Decouples "which registries" (config, HIJ-494)
/// from "how to fetch" (<see cref="HttpRegistrySource"/>, HIJ-492).
/// </summary>
public interface IRegistrySourceProvider
{
    Task<IReadOnlyList<IRegistrySource>> GetSourcesAsync(CancellationToken cancellationToken = default);
}
