namespace Outlet.Registry.Llm;

/// <summary>Options for the OpenAI adapter, bound via <c>IOptions&lt;OpenAiChatCompletionOptions&gt;</c>.</summary>
public sealed class OpenAiChatCompletionOptions
{
    public string ApiKey { get; set; } = "";

    /// <summary>Default model id used when a request does not override it (e.g. "gpt-4o-mini").</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>Default upper bound on generated tokens when a request leaves it unset.</summary>
    public int MaxOutputTokens { get; set; } = 1024;

    /// <summary>
    /// Override the API base URL — for Azure OpenAI, an OpenAI-compatible gateway, or pointing
    /// the adapter at a local stub in tests. Null uses OpenAI's default endpoint.
    /// </summary>
    public Uri? Endpoint { get; set; }
}
