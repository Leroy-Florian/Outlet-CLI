using Outlet.Core.Application.Ports;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>Hand-written <see cref="INuGetEditor"/> recording requests; outcome is configurable.</summary>
public sealed class FakeNuGetEditor : INuGetEditor
{
    private readonly List<NuGetEditRequest> _requests = [];
    private readonly List<NuGetRemoveRequest> _removeRequests = [];
    private NuGetEditResult _result = new(NuGetEditOutcome.Added);

    public IReadOnlyList<NuGetEditRequest> Requests => _requests;
    public IReadOnlyList<NuGetRemoveRequest> RemoveRequests => _removeRequests;

    public FakeNuGetEditor ReturningConflict(string warning)
    {
        _result = new NuGetEditResult(NuGetEditOutcome.Conflict, warning);
        return this;
    }

    public Task<NuGetEditResult> AddPackageAsync(NuGetEditRequest request, CancellationToken cancellationToken = default)
    {
        _requests.Add(request);
        return Task.FromResult(_result);
    }

    public Task<bool> RemovePackageAsync(NuGetRemoveRequest request, CancellationToken cancellationToken = default)
    {
        _removeRequests.Add(request);
        return Task.FromResult(true);
    }
}
