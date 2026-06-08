namespace Outlet.Registry.Llm;

/// <summary>
/// Generic chat-completion port — identical across every adapter so LLM providers stay
/// swappable. No provider specifics ever leak into this interface; a provider-specific
/// feature goes on a dedicated companion interface implemented by the same adapter.
/// </summary>
public interface IChatCompletion
{
    /// <summary>
    /// Sends <paramref name="request"/> and returns the full assistant reply as a
    /// success/failure outcome — never throws for an expected provider error
    /// (auth, rate limit, server error); those surface as <see cref="ChatResult.Failure"/>.
    /// </summary>
    Task<ChatResult> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the assistant reply incrementally as it is generated. Unlike
    /// <see cref="CompleteAsync"/>, transport/provider faults surface as exceptions while
    /// enumerating (compose retry/circuit-breaking over this port — never inside the adapter).
    /// </summary>
    IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
