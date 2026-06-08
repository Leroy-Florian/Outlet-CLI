namespace Outlet.Registry.Llm;

/// <summary>Options for the Anthropic adapter, bound via <c>IOptions&lt;AnthropicChatCompletionOptions&gt;</c>.</summary>
public sealed class AnthropicChatCompletionOptions
{
    public string ApiKey { get; set; } = "";

    /// <summary>Default model id used when a request does not override it (e.g. "claude-3-5-sonnet-latest").</summary>
    public string Model { get; set; } = "claude-3-5-sonnet-latest";

    /// <summary>
    /// Default upper bound on generated tokens when a request leaves it unset. Anthropic
    /// requires a max, so this always applies a value.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 1024;
}
