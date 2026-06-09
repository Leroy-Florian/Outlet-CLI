using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace Outlet.Registry.Resilience;

/// <summary>
/// Microsoft.Extensions.Resilience adapter for <see cref="IResilienceExecutor"/>. Instead of
/// building the pipeline by hand, it resolves the named pipeline from the DI-registered
/// <see cref="ResiliencePipelineProvider{TKey}"/> (the Microsoft-recommended composition, with
/// built-in telemetry/enrichment). Polly's <see cref="BrokenCircuitException"/> is mapped to the
/// contract's <see cref="ResilienceRejectedException"/> so callers stay provider-agnostic.
/// </summary>
public sealed class MicrosoftResilienceExecutor(ResiliencePipelineProvider<string> pipelineProvider) : IResilienceExecutor
{
    private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline(MicrosoftResilienceServiceCollectionExtensions.PipelineKey);

    public async ValueTask<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> operation, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _pipeline.ExecuteAsync(operation, cancellationToken);
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
            await _pipeline.ExecuteAsync(operation, cancellationToken);
        }
        catch (BrokenCircuitException ex)
        {
            throw new ResilienceRejectedException("The resilience circuit is open; the call was rejected.", ex);
        }
    }
}
