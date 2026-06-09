namespace Outlet.Registry.Llm;

/// <summary>
/// Provider-specific companion to <see cref="IChatCompletion"/>. It lives BESIDE the
/// generic port (never inside it) and is implemented by the very same adapter instance,
/// so generic code stays swappable while OpenAI-only features (forced JSON output) remain
/// reachable when you opt in.
/// </summary>
public interface IOpenAiChatCompletion : IChatCompletion
{
    /// <summary>
    /// Completes with OpenAI JSON mode enabled (<c>response_format = json_object</c>), so the
    /// reply is guaranteed to be syntactically valid JSON. Your prompt must still instruct the
    /// model to produce JSON.
    /// </summary>
    Task<ChatResult> CompleteJsonAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
