using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace Outlet.Registry.Llm;

/// <summary>
/// Google Gemini adapter implementing the generic <see cref="IChatCompletion"/> port.
/// System-role turns are routed to Gemini's system-instruction channel and assistant turns
/// to the "model" role. Thin by design: retries and circuit-breaking are composed over the
/// port, never embedded here.
/// </summary>
public sealed class GoogleChatCompletion(IOptions<GoogleChatCompletionOptions> options, HttpClient? httpClient = null) : IChatCompletion
{
    // Resilience is composed OVER the port (e.g. Polly), never embedded in the adapter — so the
    // SDK's own retry is switched off (empty StatusCodes = no status code is retried).
    private static readonly RequestOptions ThinRequestOptions = new(new Retry { StatusCodes = [] });

    private readonly GoogleChatCompletionOptions _options = options.Value;
    private readonly GoogleAI _googleAI = new(
        apiKey: options.Value.ApiKey,
        httpClientFactory: httpClient is null ? null : new SingleHttpClientFactory(httpClient));

    public async Task<ChatResult> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        // The Gemini SDK does not observe an already-cancelled token, so honour it up front.
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var response = await ModelFor(request).GenerateContent(BuildRequest(request), ThinRequestOptions, cancellationToken);

            var finishReason = response.Candidates?.FirstOrDefault()?.FinishReason?.ToString();
            return ChatResult.Success(response.Text ?? string.Empty, finishReason, ToUsage(response.UsageMetadata));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ChatResult.Failure(ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = ModelFor(request).GenerateContentStream(BuildRequest(request), ThinRequestOptions, cancellationToken);

        await foreach (var response in stream.WithCancellation(cancellationToken))
        {
            var delta = response.Text ?? string.Empty;
            var finishReason = response.Candidates?.FirstOrDefault()?.FinishReason?.ToString();
            if (delta.Length > 0 || finishReason is not null)
                yield return new ChatStreamChunk(delta, finishReason);
        }
    }

    private GenerativeModel ModelFor(ChatRequest request)
    {
        var systemMessages = request.Messages.Where(static m => m.Role == ChatRole.System).Select(static m => m.Content).ToList();
        var systemInstruction = systemMessages.Count == 0 ? null : new Content(string.Join("\n\n", systemMessages), "system");

        return _googleAI.GenerativeModel(model: request.Model ?? _options.Model, systemInstruction: systemInstruction);
    }

    private GenerateContentRequest BuildRequest(ChatRequest request) => new()
    {
        Contents = [.. request.Messages
            .Where(static m => m.Role != ChatRole.System)
            .Select(static m => new Content(m.Content, m.Role == ChatRole.Assistant ? "model" : "user"))],
        GenerationConfig = BuildConfig(request),
    };

    private GenerationConfig BuildConfig(ChatRequest request)
    {
        var config = new GenerationConfig { MaxOutputTokens = request.MaxOutputTokens ?? _options.MaxOutputTokens };
        if (request.Temperature is { } temperature)
            config.Temperature = (float)temperature;
        if (request.TopP is { } topP)
            config.TopP = (float)topP;
        return config;
    }

    private static ChatUsage? ToUsage(UsageMetadata? usage) =>
        usage is null
            ? null
            : new ChatUsage(usage.PromptTokenCount ?? 0, usage.CandidatesTokenCount ?? 0, usage.TotalTokenCount ?? 0);

    /// <summary>
    /// Adapts a single supplied <see cref="HttpClient"/> to the SDK's <see cref="IHttpClientFactory"/>
    /// seam — used to drive the adapter against a custom client (proxy, Polly handler, or a test stub).
    /// </summary>
    private sealed class SingleHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => httpClient;
    }
}
