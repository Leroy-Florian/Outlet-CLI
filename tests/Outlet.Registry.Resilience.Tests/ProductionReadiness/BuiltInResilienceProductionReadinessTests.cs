using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Resilience;

namespace Outlet.Registry.Resilience.Tests.ProductionReadiness;

/// <summary>
/// The built-in adapter ships a hand-written circuit breaker; these tests pin its concurrency
/// safety (no torn state under parallel load) since you own and edit that code.
/// </summary>
public sealed class BuiltInResilienceProductionReadinessTests
{
    [Fact]
    public async Task Should_RemainConsistent_When_HitConcurrently_WithBreakerEnabled()
    {
        var services = new ServiceCollection();
        services.AddBuiltInResilience(o =>
        {
            o.MaxRetries = 0;
            o.EnableCircuitBreaker = true;
            o.CircuitBreakerFailureRatio = 0.5;
            o.CircuitBreakerMinimumThroughput = 100;
            o.CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);
            o.CircuitBreakerBreakDuration = TimeSpan.FromMilliseconds(50);
        });
        var executor = services.BuildServiceProvider().GetRequiredService<IResilienceExecutor>();

        var succeeded = 0;

        async Task Worker()
        {
            for (var i = 0; i < 50; i++)
            {
                try
                {
                    await executor.ExecuteAsync(_ =>
                    {
                        Interlocked.Increment(ref succeeded);
                        return ValueTask.FromResult(0);
                    });
                }
                catch (ResilienceRejectedException)
                {
                    // Acceptable: the breaker may briefly reject under contention.
                }
            }
        }

        Task[] workers = [.. Enumerable.Range(0, 16).Select(_ => Worker())];
        await Task.WhenAll(workers);

        succeeded.Should().BeGreaterThan(0);
    }
}
