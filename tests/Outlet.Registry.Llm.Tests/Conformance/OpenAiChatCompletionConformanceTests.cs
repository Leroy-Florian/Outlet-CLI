using Microsoft.Extensions.Options;
using Outlet.Registry.Llm;
using Outlet.Registry.Llm.Tests.Support;

namespace Outlet.Registry.Llm.Tests.Conformance;

/// <summary>OpenAI adapter run against a canned chat.completion payload (level A — provider boundary mocked).</summary>
public sealed class OpenAiChatCompletionConformanceTests : ChatCompletionConformanceTests
{
    private const string CompletionBody =
        """
        {"id":"chatcmpl-stub","object":"chat.completion","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"message":{"role":"assistant","content":"Hello"},"finish_reason":"stop"}],"usage":{"prompt_tokens":3,"completion_tokens":2,"total_tokens":5}}
        """;

    private const string StreamBody =
        "data: {\"id\":\"chatcmpl-stub\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"gpt-4o-mini\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"Hel\"},\"finish_reason\":null}]}\n\n" +
        "data: {\"id\":\"chatcmpl-stub\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"gpt-4o-mini\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"lo\"},\"finish_reason\":null}]}\n\n" +
        "data: {\"id\":\"chatcmpl-stub\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"gpt-4o-mini\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n" +
        "data: [DONE]\n\n";

    protected override IChatCompletionHarness CreateHarness(bool rejecting)
    {
        var handler = new StubHttpMessageHandler(CompletionBody, StreamBody, rejecting: rejecting);
        var sender = new OpenAiChatCompletion(
            Options.Create(new OpenAiChatCompletionOptions { ApiKey = "sk-test", Model = "gpt-4o-mini" }),
            new HttpClient(handler));

        return new Harness(handler, sender);
    }

    private sealed class Harness(StubHttpMessageHandler handler, IChatCompletion sender) : IChatCompletionHarness
    {
        public IChatCompletion Sender => sender;
        public int RequestCount => handler.RequestCount;
        public void Dispose() => handler.Dispose();
    }
}
