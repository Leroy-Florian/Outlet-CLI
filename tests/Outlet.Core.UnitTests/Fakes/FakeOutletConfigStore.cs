using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>Hand-written in-memory <see cref="IOutletConfigStore"/>.</summary>
public sealed class FakeOutletConfigStore : IOutletConfigStore
{
    private OutletConfig? _config;

    public OutletConfig? Saved => _config;

    public void Seed(OutletConfig config) => _config = config;

    public bool Exists(string projectDirectory) => _config is not null;

    public Task<Result<OutletConfig>> LoadAsync(string projectDirectory, CancellationToken cancellationToken = default)
        => Task.FromResult(_config is null
            ? Result<OutletConfig>.Failure("No outlet.json found. Run 'outlet init' first.")
            : Result<OutletConfig>.Success(_config));

    public Task SaveAsync(string projectDirectory, OutletConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;
        return Task.CompletedTask;
    }
}
