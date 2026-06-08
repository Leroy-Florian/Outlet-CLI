using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Outlet.Registry.Resilience;

/// <summary>
/// Polly v8 adapter for <see cref="IResilienceExecutor"/>, backed by a
/// <see cref="ResiliencePipeline"/> built from the generic <see cref="ResilienceOptions"/>.
/// Strategy order is breaker → retry → per-attempt timeout, so an open circuit fails fast
/// without consuming retry attempts. Polly's <see cref="BrokenCircuitException"/> is mapped
/// to the contract's <see cref="ResilienceRejectedException"/> so callers stay provider-agnostic.
/// </summary>
public sealed class PollyResilienceExecutor(IOptions<ResilienceOptions> options) : IPollyResilienceExecutor
{
    public ResiliencePipeline Pipeline { get; } = Build(options.Value);

    public async ValueTask<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> operation, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Pipeline.ExecuteAsync(operation, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            throw new ResilienceRejectedException("The resilience circuit is open; the call was rejected.", ex);
        }
    }

    public async ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default)
    {
        try
        {
            await Pipeline.ExecuteAsync(operation, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            throw new ResilienceRejectedException("The resilience circuit is open; the call was rejected.", ex);
        }
    }

    private static ResiliencePipeline Build(ResilienceOptions options)
    {
        var builder = new ResiliencePipelineBuilder();

        if (options.EnableCircuitBreaker)
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitBreakerFailureRatio,
                SamplingDuration = options.CircuitBreakerSamplingDuration,
                MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                BreakDuration = options.CircuitBreakerBreakDuration,
            });
        }

        if (options.MaxRetries > 0)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetries,
                Delay = options.BaseDelay,
                BackoffType = options.Backoff switch
                {
                    ResilienceBackoff.Constant => DelayBackoffType.Constant,
                    ResilienceBackoff.Linear => DelayBackoffType.Linear,
                    _ => DelayBackoffType.Exponential,
                },
                UseJitter = options.UseJitter,
            });
        }

        if (options.AttemptTimeout is { } attemptTimeout)
            builder.AddTimeout(attemptTimeout);

        return builder.Build();
    }
}
