using Outlet.Core.Application.Ports;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>Hand-written <see cref="IPackageRestorer"/> recording requests; outcome is configurable.</summary>
public sealed class FakePackageRestorer : IPackageRestorer
{
    private readonly List<RestoreRequest> _requests = [];
    private RestoreResult _result = new(true, []);

    public IReadOnlyList<RestoreRequest> Requests => _requests;

    public FakePackageRestorer Failing(params string[] diagnostics)
    {
        _result = new RestoreResult(false, [.. diagnostics]);
        return this;
    }

    public FakePackageRestorer SucceedingWith(params string[] diagnostics)
    {
        _result = new RestoreResult(true, [.. diagnostics]);
        return this;
    }

    public Task<RestoreResult> RestoreAsync(RestoreRequest request, CancellationToken cancellationToken = default)
    {
        _requests.Add(request);
        return Task.FromResult(_result);
    }
}
