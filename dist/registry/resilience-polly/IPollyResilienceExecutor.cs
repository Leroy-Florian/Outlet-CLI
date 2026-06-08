using Polly;

namespace Outlet.Registry.Resilience;

/// <summary>
/// Provider-specific companion to <see cref="IResilienceExecutor"/>. It lives BESIDE the
/// generic port (never inside it) and is implemented by the very same adapter instance, so
/// generic code stays swappable while Polly-only features (the raw <see cref="ResiliencePipeline"/>
/// for context properties, telemetry, keyed strategies) remain reachable when you opt in.
/// </summary>
public interface IPollyResilienceExecutor : IResilienceExecutor
{
    /// <summary>The configured Polly pipeline backing this executor.</summary>
    ResiliencePipeline Pipeline { get; }
}
