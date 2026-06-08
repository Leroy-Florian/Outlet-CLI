namespace Outlet.Registry.Llm;

/// <summary>Who authored a <see cref="ChatMessage"/> in the conversation.</summary>
public enum ChatRole
{
    /// <summary>High-level instructions that steer the assistant (mapped to each provider's system channel).</summary>
    System,

    /// <summary>A message from the end user.</summary>
    User,

    /// <summary>A previous reply from the assistant, replayed to keep multi-turn context.</summary>
    Assistant,
}
