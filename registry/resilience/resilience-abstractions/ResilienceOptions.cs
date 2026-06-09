namespace Outlet.Registry.Resilience;

/// <summary>
/// Generic resilience knobs — the common ~80% case (no god-model). Every adapter binds the
/// SAME options type and maps it onto its provider, so swapping providers is a one-line DI
/// change. Provider-only tuning lives on a companion interface or by editing your copy.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>Number of retry attempts after the initial try. 0 disables retries.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay before the first retry; grown according to <see cref="Backoff"/>.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>How the retry delay grows between attempts.</summary>
    public ResilienceBackoff Backoff { get; set; } = ResilienceBackoff.Exponential;

    /// <summary>Add randomised jitter to retry delays to avoid synchronised retry storms.</summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>Per-attempt timeout. <see langword="null"/> disables the timeout strategy.</summary>
    public TimeSpan? AttemptTimeout { get; set; }

    /// <summary>Enable the circuit breaker. When open, calls are rejected with <see cref="ResilienceRejectedException"/>.</summary>
    public bool EnableCircuitBreaker { get; set; }

    /// <summary>Failure ratio in <c>[0, 1]</c> over the sampling window that trips the breaker.</summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>Rolling window the failure ratio is measured over.</summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Minimum number of calls observed in the window before the breaker can trip.</summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>How long the breaker stays open before it probes the dependency again.</summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(15);
}
