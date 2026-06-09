namespace Outlet.Registry.Llm;

/// <summary>Options for the Google Gemini adapter, bound via <c>IOptions&lt;GoogleChatCompletionOptions&gt;</c>.</summary>
public sealed class GoogleChatCompletionOptions
{
    public string ApiKey { get; set; } = "";

    /// <summary>Default model id used when a request does not override it (e.g. "gemini-1.5-flash").</summary>
    public string Model { get; set; } = "gemini-1.5-flash";

    /// <summary>Default upper bound on generated tokens when a request leaves it unset.</summary>
    public int MaxOutputTokens { get; set; } = 1024;
}
