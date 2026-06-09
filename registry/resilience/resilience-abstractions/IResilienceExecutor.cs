namespace Outlet.Registry.Resilience;

/// <summary>
/// Generic resilience port — identical across every adapter so providers stay swappable.
/// It wraps an arbitrary operation with the configured strategies (retry, timeout, circuit
/// breaker). Resilience is COMPOSED over other ports (e.g. an IEmailSender), never embedded
/// inside their adapters — keeping adapters thin and the policy in one swappable place.
/// No provider specifics ever leak into this interface; a provider-only feature goes on a
/// dedicated companion interface implemented by the same adapter.
/// </summary>
public interface IResilienceExecutor
{
    /// <summary>Executes <paramref name="operation"/> under the configured strategies and returns its result.</summary>
    /// <exception cref="ResilienceRejectedException">The call was rejected rather than completed (typically the circuit is open).</exception>
    ValueTask<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>Executes a result-less <paramref name="operation"/> under the configured strategies.</summary>
    /// <exception cref="ResilienceRejectedException">The call was rejected rather than completed (typically the circuit is open).</exception>
    ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default);
}
