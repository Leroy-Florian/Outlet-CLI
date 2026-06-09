namespace Outlet.Registry.Llm;

/// <summary>
/// Outcome of a completion. Expected provider failures (auth, rate limit, server error)
/// are modelled here rather than thrown, so calling code can branch without try/catch.
/// </summary>
public sealed class ChatResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    /// <summary>The assistant's reply text; empty when the call failed.</summary>
    public string Content { get; }

    /// <summary>Provider-reported reason the generation stopped (e.g. "stop", "length"), when available.</summary>
    public string? FinishReason { get; }

    /// <summary>Token usage when the provider reported it.</summary>
    public ChatUsage? Usage { get; }

    /// <summary>Human-readable reason when the call failed.</summary>
    public string? Error { get; }

    private ChatResult(bool isSuccess, string content, string? finishReason, ChatUsage? usage, string? error)
    {
        IsSuccess = isSuccess;
        Content = content;
        FinishReason = finishReason;
        Usage = usage;
        Error = error;
    }

    public static ChatResult Success(string content, string? finishReason = null, ChatUsage? usage = null)
        => new(true, content, finishReason, usage, null);

    public static ChatResult Failure(string error)
        => new(false, string.Empty, null, null, error);
}
