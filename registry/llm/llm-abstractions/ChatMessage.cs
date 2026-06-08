namespace Outlet.Registry.Llm;

/// <summary>A single turn in the conversation: who said it and the text they said.</summary>
public sealed record ChatMessage(ChatRole Role, string Content)
{
    /// <summary>Convenience factory for a system-instruction message.</summary>
    public static ChatMessage System(string content) => new(ChatRole.System, content);

    /// <summary>Convenience factory for a user message.</summary>
    public static ChatMessage User(string content) => new(ChatRole.User, content);

    /// <summary>Convenience factory for a prior assistant message.</summary>
    public static ChatMessage Assistant(string content) => new(ChatRole.Assistant, content);
}
