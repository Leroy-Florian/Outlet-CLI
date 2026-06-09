using FluentAssertions;
using Outlet.Core.Application.Cli;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.Cli;

public sealed class SelfUpdateCliUseCaseTests
{
    private readonly FakeCliUpdater _updater = new();

    private SelfUpdateCliUseCase BuildUseCase() => new(_updater);

    [Fact]
    public async Task Should_Succeed_When_UpdaterReportsSuccess()
    {
        _updater.SeedOutcome(succeeded: true, "Outlet.Cli updated to 0.2.0.");

        var result = await BuildUseCase().HandleAsync(new SelfUpdateCliCommand());

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value.Should().Be("Outlet.Cli updated to 0.2.0.");
    }

    [Fact]
    public async Task Should_Fail_When_UpdaterReportsFailure()
    {
        _updater.SeedOutcome(succeeded: false, "tool 'outlet.cli' is not installed.");

        var result = await BuildUseCase().HandleAsync(new SelfUpdateCliCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("tool 'outlet.cli' is not installed.");
    }
}
