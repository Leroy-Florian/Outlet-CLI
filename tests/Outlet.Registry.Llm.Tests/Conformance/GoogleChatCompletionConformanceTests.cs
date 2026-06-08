using Microsoft.Extensions.Options;
using Outlet.Registry.Llm;
using Outlet.Registry.Llm.Tests.Support;

namespace Outlet.Registry.Llm.Tests.Conformance;

/// <summary>Gemini adapter run against a canned generateContent payload (level A — provider boundary mocked).</summary>
public sealed class GoogleChatCompletionConformanceTests : ChatCompletionConformanceTests
{
    private const string CompletionBody =
        """
        {"candidates":[{"content":{"parts":[{"text":"Hello"}],"role":"model"},"finishReason":"STOP","index":0}],"usageMetadata":{"promptTokenCount":3,"candidatesTokenCount":2,"totalTokenCount":5}}
        """;

    // streamGenerateContent (without alt=sse) returns a JSON array of GenerateContentResponse chunks.
    private const string StreamBody =
        """
        [{"candidates":[{"content":{"parts":[{"text":"Hel"}],"role":"model"},"index":0}]}
        ,
        {"candidates":[{"content":{"parts":[{"text":"lo"}],"role":"model"},"finishReason":"STOP","index":0}],"usageMetadata":{"promptTokenCount":3,"candidatesTokenCount":2,"totalTokenCount":5}}
        ]
        """;

    protected override IChatCompletionHarness CreateHarness(bool rejecting)
    {
        var handler = new StubHttpMessageHandler(CompletionBody, StreamBody, streamContentType: "application/json", rejecting: rejecting);
        // Gemini keys are exactly 39 chars ("AIzaSy…"); the SDK guards the format before any HTTP call.
        var sender = new GoogleChatCompletion(
            Options.Create(new GoogleChatCompletionOptions { ApiKey = "AIzaSyB000000000000000000000000000000ut", Model = "gemini-1.5-flash" }),
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
