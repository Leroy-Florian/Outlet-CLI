using Outlet.Core.Infrastructure.Registry;

namespace Outlet.Core.Infrastructure.UnitTests.Fakes;

/// <summary>Hand-written <see cref="IRegistrySourceProvider"/> returning a fixed source list.</summary>
public sealed class FakeRegistrySourceProvider(params IRegistrySource[] sources) : IRegistrySourceProvider
{
    public Task<IReadOnlyList<IRegistrySource>> GetSourcesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<IRegistrySource>>([.. sources]);
}
