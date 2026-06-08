using System.Runtime.CompilerServices;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Llm;

/// <summary>
/// Anthropic (Claude) adapter implementing the generic <see cref="IChatCompletion"/> port.
/// System-role turns are routed to Anthropic's dedicated system channel. Thin by design:
/// retries and circuit-breaking are composed over the port, never embedded here.
/// </summary>
public sealed class AnthropicChatCompletion(IOptions<AnthropicChatCompletionOptions> options, HttpClient? httpClient = null) : IChatCompletion
{
    private readonly AnthropicChatCompletionOptions _options = options.Value;
    private readonly AnthropicClient _client = new(new APIAuthentication(options.Value.ApiKey), httpClient);

    public async Task<ChatResult> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Messages.GetClaudeMessageAsync(BuildParameters(request, stream: false), cancellationToken);

            var text = string.Concat(response.Content.OfType<TextContent>().Select(static c => c.Text));
            return ChatResult.Success(text, response.StopReason, ToUsage(response.Usage));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ChatResult.Failure(ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stream = _client.Messages.StreamClaudeMessageAsync(BuildParameters(request, stream: true), cancellationToken);

        await foreach (var response in stream.WithCancellation(cancellationToken))
        {
            var delta = response.Delta?.Text ?? string.Empty;
            var finishReason = response.Delta?.StopReason;
            if (delta.Length > 0 || finishReason is not null)
                yield return new ChatStreamChunk(delta, finishReason);
        }
    }

    private MessageParameters BuildParameters(ChatRequest request, bool stream)
    {
        var parameters = new MessageParameters
        {
            Model = request.Model ?? _options.Model,
            MaxTokens = request.MaxOutputTokens ?? _options.MaxOutputTokens,
            Stream = stream,
            Messages = [.. request.Messages
                .Where(static m => m.Role != ChatRole.System)
                .Select(static m => new Message(
                    m.Role == ChatRole.Assistant ? RoleType.Assistant : RoleType.User,
                    m.Content))],
            System = [.. request.Messages
                .Where(static m => m.Role == ChatRole.System)
                .Select(static m => new SystemMessage(m.Content))],
        };

        if (request.Temperature is { } temperature)
            parameters.Temperature = (decimal)temperature;
        if (request.TopP is { } topP)
            parameters.TopP = (decimal)topP;

        return parameters;
    }

    private static ChatUsage? ToUsage(Usage? usage) =>
        usage is null ? null : new ChatUsage(usage.InputTokens, usage.OutputTokens, usage.InputTokens + usage.OutputTokens);
}
