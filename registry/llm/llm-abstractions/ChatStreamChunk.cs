namespace Outlet.Registry.Llm;

/// <summary>
/// One incremental slice of a streamed completion: the text produced since the previous
/// chunk, plus the finish reason on the terminal chunk when the provider sends one.
/// Concatenating every <see cref="ContentDelta"/> reconstructs the full reply.
/// </summary>
public sealed record ChatStreamChunk(string ContentDelta, string? FinishReason = null);
