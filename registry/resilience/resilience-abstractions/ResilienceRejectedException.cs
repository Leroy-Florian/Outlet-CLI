namespace Outlet.Registry.Resilience;

/// <summary>
/// Thrown by an <see cref="IResilienceExecutor"/> when a call is rejected instead of being
/// executed to completion — most commonly because the circuit breaker is open. A single
/// generic type so calling code branches on rejection WITHOUT referencing a provider-specific
/// exception, which is what keeps adapters swappable (every adapter maps its own rejection
/// onto this type).
/// </summary>
public sealed class ResilienceRejectedException : Exception
{
    public ResilienceRejectedException(string message)
        : base(message)
    {
    }

    public ResilienceRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
