using Microsoft.Extensions.Options;
using Outlet.Registry.Llm;
using Outlet.Registry.Llm.Tests.Support;

namespace Outlet.Registry.Llm.Tests.Conformance;

/// <summary>Anthropic adapter run against a canned messages payload (level A — provider boundary mocked).</summary>
public sealed class AnthropicChatCompletionConformanceTests : ChatCompletionConformanceTests
{
    private const string CompletionBody =
        """
        {"id":"msg_stub","type":"message","role":"assistant","model":"claude-3-5-sonnet-latest","content":[{"type":"text","text":"Hello"}],"stop_reason":"end_turn","stop_sequence":null,"usage":{"input_tokens":3,"output_tokens":2}}
        """;

    private const string StreamBody =
        "event: message_start\n" +
        "data: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_stub\",\"type\":\"message\",\"role\":\"assistant\",\"model\":\"claude-3-5-sonnet-latest\",\"content\":[],\"stop_reason\":null,\"usage\":{\"input_tokens\":3,\"output_tokens\":0}}}\n\n" +
        "event: content_block_start\n" +
        "data: {\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"text\",\"text\":\"\"}}\n\n" +
        "event: content_block_delta\n" +
        "data: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"Hel\"}}\n\n" +
        "event: content_block_delta\n" +
        "data: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"lo\"}}\n\n" +
        "event: content_block_stop\n" +
        "data: {\"type\":\"content_block_stop\",\"index\":0}\n\n" +
        "event: message_delta\n" +
        "data: {\"type\":\"message_delta\",\"delta\":{\"stop_reason\":\"end_turn\"},\"usage\":{\"output_tokens\":2}}\n\n" +
        "event: message_stop\n" +
        "data: {\"type\":\"message_stop\"}\n\n";

    protected override IChatCompletionHarness CreateHarness(bool rejecting)
    {
        var handler = new StubHttpMessageHandler(CompletionBody, StreamBody, rejecting: rejecting);
        var sender = new AnthropicChatCompletion(
            Options.Create(new AnthropicChatCompletionOptions { ApiKey = "sk-ant-test", Model = "claude-3-5-sonnet-latest" }),
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
