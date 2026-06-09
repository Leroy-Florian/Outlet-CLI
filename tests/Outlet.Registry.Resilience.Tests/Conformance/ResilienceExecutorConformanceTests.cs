using Outlet.Registry.Resilience;

namespace Outlet.Registry.Resilience.Tests.Conformance;

/// <summary>
/// Reusable PORT conformance suite — "every IResilienceExecutor must behave this way".
/// Each adapter runs it against its own DI wiring, so swappability is TESTED, not asserted:
/// retry, exception propagation, cancellation and (crucially) the generic
/// <see cref="ResilienceRejectedException"/> on an open circuit all behave identically.
/// </summary>
public abstract class ResilienceExecutorConformanceTests
{
    /// <summary>Builds the adapter under test wired to the given generic options.</summary>
    protected abstract IResilienceExecutor CreateExecutor(Action<ResilienceOptions> configure);

    [Fact]
    public async Task Should_ReturnResult_When_OperationSucceedsFirstTry()
    {
        var executor = CreateExecutor(NoRetries);
        var calls = 0;

        var result = await executor.ExecuteAsync(_ =>
        {
            calls++;
            return ValueTask.FromResult(42);
        });

        result.Should().Be(42);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Should_RetryThenSucceed_When_OperationFailsTransiently()
    {
        var executor = CreateExecutor(FastRetries);
        var calls = 0;

        var result = await executor.ExecuteAsync(_ =>
        {
            calls++;
            if (calls <= 2)
                throw new InvalidOperationException("transient");

            return ValueTask.FromResult(7);
        });

        result.Should().Be(7);
        calls.Should().Be(3);
    }

    [Fact]
    public async Task Should_PropagateOperationException_When_RetriesExhausted()
    {
        var executor = CreateExecutor(o =>
        {
            FastRetries(o);
            o.MaxRetries = 2;
        });
        var calls = 0;

        var act = async () => await executor.ExecuteAsync<int>(_ =>
        {
            calls++;
            throw new InvalidOperationException("boom");
        });

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("boom");
        calls.Should().Be(3);
    }

    [Fact]
    public async Task Should_HonorCancellation()
    {
        var executor = CreateExecutor(NoRetries);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await executor.ExecuteAsync(ct =>
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(1);
        }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_RejectCalls_With_GenericException_When_CircuitOpens()
    {
        var executor = CreateExecutor(o =>
        {
            o.MaxRetries = 0;
            o.EnableCircuitBreaker = true;
            o.CircuitBreakerFailureRatio = 0.5;
            o.CircuitBreakerMinimumThroughput = 2;
            o.CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);
            o.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);
        });

        var rejected = false;
        for (var i = 0; i < 20 && !rejected; i++)
        {
            try
            {
                await executor.ExecuteAsync<int>(_ => throw new InvalidOperationException("down"));
            }
            catch (ResilienceRejectedException)
            {
                rejected = true;
            }
            catch (InvalidOperationException)
            {
            }
        }

        rejected.Should().BeTrue("an open circuit must reject calls with the generic ResilienceRejectedException");
    }

    [Fact]
    public async Task Should_ExecuteVoidOverload_When_OperationSucceeds()
    {
        var executor = CreateExecutor(NoRetries);
        var calls = 0;

        await executor.ExecuteAsync(_ =>
        {
            calls++;
            return ValueTask.CompletedTask;
        });

        calls.Should().Be(1);
    }

    private static void NoRetries(ResilienceOptions options)
        => options.MaxRetries = 0;

    private static void FastRetries(ResilienceOptions options)
    {
        options.MaxRetries = 3;
        options.BaseDelay = TimeSpan.FromMilliseconds(1);
        options.Backoff = ResilienceBackoff.Constant;
        options.UseJitter = false;
    }
}
