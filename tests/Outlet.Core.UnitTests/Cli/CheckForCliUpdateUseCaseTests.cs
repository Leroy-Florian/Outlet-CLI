using FluentAssertions;
using Outlet.Core.Application.Cli;
using Outlet.Core.Domain.Cli;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.Cli;

public sealed class CheckForCliUpdateUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeCliReleaseClient _releaseClient = new();
    private readonly FakeCliUpdateStateStore _stateStore = new();
    private readonly FixedDateTimeProvider _clock = new(Now);

    private CheckForCliUpdateUseCase BuildUseCase() => new(_releaseClient, _stateStore, _clock);

    private Task<Outlet.Kernel.Shared.Result<CliUpdateStatus>> CheckAsync(string current, bool force = false)
        => BuildUseCase().HandleAsync(new CheckForCliUpdateQuery(CliVersion.From(current), force));

    [Fact]
    public async Task Should_ReportAvailable_When_FeedHasNewerVersion()
    {
        _releaseClient.SeedLatest("0.2.0");

        var result = await CheckAsync("0.1.0");

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.UpdateAvailable.Should().BeTrue();
        result.Value.LatestVersion.Should().Be("0.2.0");
        result.Value.CurrentVersion.Should().Be("0.1.0");
    }

    [Fact]
    public async Task Should_NotReportAvailable_When_CurrentIsLatest()
    {
        _releaseClient.SeedLatest("0.1.0");

        var result = await CheckAsync("0.1.0");

        result.Value!.UpdateAvailable.Should().BeFalse();
        result.Value.LatestVersion.Should().Be("0.1.0");
    }

    [Fact]
    public async Task Should_RecordCheckTimestamp_When_FeedWasReached()
    {
        _releaseClient.SeedLatest("0.2.0");

        await CheckAsync("0.1.0");

        _stateStore.LastCheckUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Should_SkipFeed_When_CheckedWithinThrottleWindow()
    {
        _stateStore.Seed(Now - TimeSpan.FromHours(1));
        _releaseClient.SeedLatest("0.2.0");

        var result = await CheckAsync("0.1.0");

        result.Value!.UpdateAvailable.Should().BeFalse();
        result.Value.LatestVersion.Should().BeNull();
        _releaseClient.Calls.Should().Be(0, "a recent check must not touch the feed");
    }

    [Fact]
    public async Task Should_QueryFeed_When_LastCheckIsStale()
    {
        _stateStore.Seed(Now - TimeSpan.FromHours(25));
        _releaseClient.SeedLatest("0.2.0");

        var result = await CheckAsync("0.1.0");

        result.Value!.UpdateAvailable.Should().BeTrue();
        _releaseClient.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Should_QueryFeed_When_Forced_EvenIfRecentlyChecked()
    {
        _stateStore.Seed(Now - TimeSpan.FromMinutes(5));
        _releaseClient.SeedLatest("0.2.0");

        var result = await CheckAsync("0.1.0", force: true);

        result.Value!.UpdateAvailable.Should().BeTrue();
        _releaseClient.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnUnknown_When_FeedIsUnreachable()
    {
        _releaseClient.SeedUnreachable();

        var result = await CheckAsync("0.1.0");

        result.IsSuccess.Should().BeTrue();
        result.Value!.UpdateAvailable.Should().BeFalse();
        result.Value.LatestVersion.Should().BeNull();
    }

    [Fact]
    public async Task Should_NotArmThrottle_When_FeedIsUnreachable()
    {
        _releaseClient.SeedUnreachable();

        await CheckAsync("0.1.0");

        _stateStore.LastCheckUtc.Should().BeNull("a failed lookup should be retried next time");
    }
}
