using System.ClientModel;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using OpenAI;
using OAI = OpenAI.Chat;

namespace Outlet.Registry.Llm;

/// <summary>
/// OpenAI adapter implementing both the generic <see cref="IChatCompletion"/> and the
/// provider-specific <see cref="IOpenAiChatCompletion"/> from one class — registered once
/// and forwarded to both interfaces (see AddOpenAiChatCompletion). Thin by design: retries
/// and circuit-breaking are composed over the port, never embedded here.
/// </summary>
public sealed class OpenAiChatCompletion(IOptions<OpenAiChatCompletionOptions> options, HttpClient? httpClient = null) : IOpenAiChatCompletion
{
    private readonly OpenAiChatCompletionOptions _options = options.Value;
    private readonly ApiKeyCredential _credential = new(options.Value.ApiKey);
    private readonly OpenAIClientOptions _clientOptions = BuildClientOptions(options.Value, httpClient);

    public Task<ChatResult> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
        => CompleteCoreAsync(request, jsonMode: false, cancellationToken);

    public Task<ChatResult> CompleteJsonAsync(ChatRequest request, CancellationToken cancellationToken = default)
        => CompleteCoreAsync(request, jsonMode: true, cancellationToken);

    private async Task<ChatResult> CompleteCoreAsync(ChatRequest request, bool jsonMode, CancellationToken cancellationToken)
    {
        try
        {
            ClientResult<OAI.ChatCompletion> result = await ClientFor(request.Model)
                .CompleteChatAsync(BuildMessages(request), BuildOptions(request, jsonMode), cancellationToken);

            var completion = result.Value;
            return ChatResult.Success(ExtractText(completion.Content), completion.FinishReason.ToString(), ToUsage(completion.Usage));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ChatResult.Failure(ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var updates = ClientFor(request.Model)
            .CompleteChatStreamingAsync(BuildMessages(request), BuildOptions(request, jsonMode: false), cancellationToken);

        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            var delta = ExtractText(update.ContentUpdate);
            var finishReason = update.FinishReason?.ToString();
            if (delta.Length > 0 || finishReason is not null)
                yield return new ChatStreamChunk(delta, finishReason);
        }
    }

    private OAI.ChatClient ClientFor(string? model) => new(model ?? _options.Model, _credential, _clientOptions);

    private static OpenAIClientOptions BuildClientOptions(OpenAiChatCompletionOptions options, HttpClient? httpClient)
    {
        var clientOptions = new OpenAIClientOptions();
        if (options.Endpoint is not null)
            clientOptions.Endpoint = options.Endpoint;
        if (httpClient is not null)
            clientOptions.Transport = new HttpClientPipelineTransport(httpClient);
        return clientOptions;
    }

    private static List<OAI.ChatMessage> BuildMessages(ChatRequest request) =>
        [.. request.Messages.Select(static m => m.Role switch
        {
            ChatRole.System => (OAI.ChatMessage)new OAI.SystemChatMessage(m.Content),
            ChatRole.Assistant => new OAI.AssistantChatMessage(m.Content),
            _ => new OAI.UserChatMessage(m.Content),
        })];

    private OAI.ChatCompletionOptions BuildOptions(ChatRequest request, bool jsonMode)
    {
        var options = new OAI.ChatCompletionOptions
        {
            MaxOutputTokenCount = request.MaxOutputTokens ?? _options.MaxOutputTokens,
        };
        if (request.Temperature is { } temperature)
            options.Temperature = (float)temperature;
        if (request.TopP is { } topP)
            options.TopP = (float)topP;
        if (jsonMode)
            options.ResponseFormat = OAI.ChatResponseFormat.CreateJsonObjectFormat();
        return options;
    }

    private static string ExtractText(OAI.ChatMessageContent content) =>
        string.Concat(content.Where(static p => p.Kind == OAI.ChatMessageContentPartKind.Text).Select(static p => p.Text));

    private static ChatUsage? ToUsage(OAI.ChatTokenUsage? usage) =>
        usage is null ? null : new ChatUsage(usage.InputTokenCount, usage.OutputTokenCount, usage.TotalTokenCount);
}
