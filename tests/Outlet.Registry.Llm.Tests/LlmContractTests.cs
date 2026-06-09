using Outlet.Registry.Llm;

namespace Outlet.Registry.Llm.Tests;

public sealed class LlmContractTests
{
    [Fact]
    public void Should_DefaultOptionalKnobsToNull_When_OnlyMessagesAreSet()
    {
        var request = new ChatRequest
        {
            Messages = [ChatMessage.User("Hello")],
        };

        request.Model.Should().BeNull();
        request.MaxOutputTokens.Should().BeNull();
        request.Temperature.Should().BeNull();
        request.TopP.Should().BeNull();
    }

    [Fact]
    public void Should_TagRole_When_BuiltViaFactories()
    {
        ChatMessage.System("s").Role.Should().Be(ChatRole.System);
        ChatMessage.User("u").Role.Should().Be(ChatRole.User);
        ChatMessage.Assistant("a").Role.Should().Be(ChatRole.Assistant);
    }

    [Fact]
    public void Should_CarryContentAndUsage_When_ResultIsSuccess()
    {
        var result = ChatResult.Success("hi", "stop", new ChatUsage(3, 2, 5));

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Content.Should().Be("hi");
        result.FinishReason.Should().Be("stop");
        result.Usage.Should().Be(new ChatUsage(3, 2, 5));
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Should_CarryError_And_EmptyContent_When_ResultIsFailure()
    {
        var result = ChatResult.Failure("rate limited");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("rate limited");
        result.Content.Should().BeEmpty();
        result.Usage.Should().BeNull();
    }

    [Fact]
    public void Should_ReconstructReply_When_StreamChunksAreConcatenated()
    {
        ChatStreamChunk[] chunks = [new("Hel"), new("lo", "stop")];

        string.Concat(chunks.Select(c => c.ContentDelta)).Should().Be("Hello");
        chunks[^1].FinishReason.Should().Be("stop");
    }
}
