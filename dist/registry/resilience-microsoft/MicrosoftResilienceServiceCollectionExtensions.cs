using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Outlet.Registry.Resilience;

/// <summary>
/// DI wiring for the Microsoft.Extensions.Resilience adapter. Registers a named pipeline (built
/// from the generic <see cref="ResilienceOptions"/>) plus Microsoft's resilience telemetry
/// enricher. Swap to another provider by changing this one call.
/// </summary>
public static class MicrosoftResilienceServiceCollectionExtensions
{
    /// <summary>Key under which the generic pipeline is registered in the Microsoft pipeline registry.</summary>
    public const string PipelineKey = "outlet-resilience";

    public static IServiceCollection AddMicrosoftResilience(this IServiceCollection services, Action<ResilienceOptions> configure)
    {
        services.Configure(configure);

        // Microsoft's telemetry enrichment (exception/result metadata on resilience events).
        services.AddResilienceEnricher();

        services.AddResiliencePipeline(PipelineKey, static (builder, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<ResilienceOptions>>().Value;

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
        });

        services.AddSingleton<IResilienceExecutor, MicrosoftResilienceExecutor>();

        return services;
    }
}
