namespace Outlet.Registry.Llm;

/// <summary>
/// Generic chat-completion request — the common ~80% case (no god-model). Sampling knobs
/// are optional; when left null the adapter falls back to its configured defaults. The
/// system prompt is just a <see cref="ChatRole.System"/> message, routed to each provider's
/// system channel by the adapter.
/// </summary>
public sealed class ChatRequest
{
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    /// <summary>Override the adapter's configured default model for this call.</summary>
    public string? Model { get; init; }

    /// <summary>Upper bound on tokens to generate; null uses the adapter's configured default.</summary>
    public int? MaxOutputTokens { get; init; }

    /// <summary>Sampling temperature (provider range); null uses the provider default.</summary>
    public double? Temperature { get; init; }

    /// <summary>Nucleus-sampling probability mass; null uses the provider default.</summary>
    public double? TopP { get; init; }
}
