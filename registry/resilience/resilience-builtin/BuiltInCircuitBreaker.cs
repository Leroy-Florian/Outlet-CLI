namespace Outlet.Registry.Resilience;

/// <summary>
/// A small, thread-safe failure-ratio circuit breaker written by hand (no provider package).
/// Closed → counts outcomes over a rolling sampling window; once throughput and failure ratio
/// cross their thresholds it trips Open and rejects calls for the break duration; it then probes
/// HalfOpen, closing on a success or re-opening on a failure. Records ONE outcome per executed
/// call (the breaker sits outside the retry loop), so an open circuit fails fast.
/// </summary>
internal sealed class BuiltInCircuitBreaker(ResilienceOptions options)
{
    private readonly object _gate = new();

    private CircuitState _state = CircuitState.Closed;
    private int _calls;
    private int _failures;
    private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;
    private DateTimeOffset _openedAt;

    /// <summary>Throws <see cref="ResilienceRejectedException"/> when the circuit is open and still cooling down.</summary>
    public void EnsureCallPermitted()
    {
        if (!options.EnableCircuitBreaker)
            return;

        lock (_gate)
        {
            if (_state != CircuitState.Open)
                return;

            if (DateTimeOffset.UtcNow - _openedAt < options.CircuitBreakerBreakDuration)
                throw new ResilienceRejectedException("The resilience circuit is open; the call was rejected.");

            // Cooldown elapsed: allow a single probing call.
            _state = CircuitState.HalfOpen;
        }
    }

    public void OnSuccess()
    {
        if (!options.EnableCircuitBreaker)
            return;

        lock (_gate)
        {
            if (_state == CircuitState.HalfOpen)
            {
                CloseAndReset();
                return;
            }

            Record(success: true);
        }
    }

    public void OnFailure()
    {
        if (!options.EnableCircuitBreaker)
            return;

        lock (_gate)
        {
            if (_state == CircuitState.HalfOpen)
            {
                Open();
                return;
            }

            Record(success: false);

            if (_calls >= options.CircuitBreakerMinimumThroughput
                && (double)_failures / _calls >= options.CircuitBreakerFailureRatio)
            {
                Open();
            }
        }
    }

    private void Record(bool success)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _windowStart >= options.CircuitBreakerSamplingDuration)
        {
            _windowStart = now;
            _calls = 0;
            _failures = 0;
        }

        _calls++;
        if (!success)
            _failures++;
    }

    private void Open()
    {
        _state = CircuitState.Open;
        _openedAt = DateTimeOffset.UtcNow;
    }

    private void CloseAndReset()
    {
        _state = CircuitState.Closed;
        _windowStart = DateTimeOffset.UtcNow;
        _calls = 0;
        _failures = 0;
    }

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen,
    }
}
