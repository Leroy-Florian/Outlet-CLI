using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Resilience;

namespace Outlet.Registry.Resilience.Tests;

public sealed class ResilienceContractTests
{
    [Fact]
    public void Should_HaveSensibleDefaults_OnGenericOptions()
    {
        var options = new ResilienceOptions();

        options.MaxRetries.Should().Be(3);
        options.Backoff.Should().Be(ResilienceBackoff.Exponential);
        options.UseJitter.Should().BeTrue();
        options.EnableCircuitBreaker.Should().BeFalse();
        options.AttemptTimeout.Should().BeNull();
    }

    [Fact]
    public void Should_ExposeRawPipeline_When_UsingPollyCompanion()
    {
        var services = new ServiceCollection();
        services.AddPollyResilience(o => o.MaxRetries = 1);

        using var provider = services.BuildServiceProvider();
        var specific = provider.GetRequiredService<IPollyResilienceExecutor>();

        specific.Pipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_TimeOutTheAttempt_When_AttemptTimeoutElapses()
    {
        var services = new ServiceCollection();
        services.AddBuiltInResilience(o =>
        {
            o.MaxRetries = 0;
            o.AttemptTimeout = TimeSpan.FromMilliseconds(50);
        });
        var executor = services.BuildServiceProvider().GetRequiredService<IResilienceExecutor>();

        var act = async () => await executor.ExecuteAsync(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return 1;
        });

        await act.Should().ThrowAsync<TimeoutException>();
    }
}
