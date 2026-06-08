using Microsoft.Extensions.Options;

namespace Outlet.Registry.Resilience;

/// <summary>
/// Zero-dependency adapter for <see cref="IResilienceExecutor"/> — retry with backoff, a
/// per-attempt timeout and a circuit breaker, all written by hand with NO provider package.
/// It embodies Outlet's ownership ethos: you copy it in and own 100% of the resilience code.
/// Behaviour matches the Polly/Microsoft adapters so it is a drop-in swap (verified by the
/// shared port conformance suite).
/// </summary>
public sealed class BuiltInResilienceExecutor(IOptions<ResilienceOptions> options) : IResilienceExecutor
{
    private readonly ResilienceOptions _options = options.Value;
    private readonly BuiltInCircuitBreaker _breaker = new(options.Value);

    public async ValueTask<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> operation, CancellationToken cancellationToken = default)
    {
        // Breaker sits OUTSIDE the retry loop: an open circuit fails fast, one outcome per call.
        _breaker.EnsureCallPermitted();

        try
        {
            var result = await RunWithRetriesAsync(operation, cancellationToken);
            _breaker.OnSuccess();
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ResilienceRejectedException)
        {
            _breaker.OnFailure();
            throw;
        }
    }

    public async ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default)
        => await ExecuteAsync<object?>(
            async ct =>
            {
                await operation(ct);
                return null;
            },
            cancellationToken);

    private async ValueTask<T> RunWithRetriesAsync<T>(Func<CancellationToken, ValueTask<T>> operation, CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await RunWithTimeoutAsync(operation, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < _options.MaxRetries)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken);
                attempt++;
            }
        }
    }

    private async ValueTask<T> RunWithTimeoutAsync<T>(Func<CancellationToken, ValueTask<T>> operation, CancellationToken cancellationToken)
    {
        if (_options.AttemptTimeout is not { } timeout)
            return await operation(cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            return await operation(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // The attempt (not the caller) timed out: surface it as a retryable failure.
            throw new TimeoutException($"The operation exceeded its {timeout} attempt timeout.");
        }
    }

    private async ValueTask DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var delay = _options.Backoff switch
        {
            ResilienceBackoff.Constant => _options.BaseDelay,
            ResilienceBackoff.Linear => _options.BaseDelay * (attempt + 1),
            _ => _options.BaseDelay * Math.Pow(2, attempt),
        };

        if (_options.UseJitter)
            delay *= 0.75 + (Random.Shared.NextDouble() * 0.5);

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);
    }
}
