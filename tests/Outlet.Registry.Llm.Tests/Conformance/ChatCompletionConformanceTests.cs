using Outlet.Registry.Llm;

namespace Outlet.Registry.Llm.Tests.Conformance;

/// <summary>
/// A configured <see cref="IChatCompletion"/> wired to its hermetic double, plus the number
/// of HTTP requests the double received.
/// </summary>
public interface IChatCompletionHarness : IDisposable
{
    IChatCompletion Sender { get; }
    int RequestCount { get; }
}

/// <summary>
/// Reusable PORT conformance suite — "every IChatCompletion must behave this way". Each adapter
/// runs it against its own provider double (canned OpenAI / Anthropic / Gemini wire payloads),
/// so swappability is TESTED, not asserted.
/// </summary>
public abstract class ChatCompletionConformanceTests
{
    protected abstract IChatCompletionHarness CreateHarness(bool rejecting);

    [Fact]
    public async Task Should_ReturnAssistantText_And_CallProviderOnce_When_ProviderResponds()
    {
        using var harness = CreateHarness(rejecting: false);

        var result = await harness.Sender.CompleteAsync(SampleRequest());

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Content.Should().Be("Hello");
        result.Usage.Should().NotBeNull();
        result.Usage!.OutputTokens.Should().Be(2);
        harness.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnFailure_NotThrow_When_ProviderRejects()
    {
        using var harness = CreateHarness(rejecting: true);

        var result = await harness.Sender.CompleteAsync(SampleRequest());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrEmpty();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_HonorCancellation()
    {
        using var harness = CreateHarness(rejecting: false);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await harness.Sender.CompleteAsync(SampleRequest(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_StreamIncrementalText_That_ConcatenatesToFullReply()
    {
        using var harness = CreateHarness(rejecting: false);

        List<ChatStreamChunk> chunks = [];
        await foreach (var chunk in harness.Sender.StreamAsync(SampleRequest()))
            chunks.Add(chunk);

        chunks.Should().NotBeEmpty();
        string.Concat(chunks.Select(c => c.ContentDelta)).Should().Be("Hello");
    }

    protected static ChatRequest SampleRequest() => new()
    {
        Messages =
        [
            ChatMessage.System("You are a test."),
            ChatMessage.User("Say hello."),
        ],
        MaxOutputTokens = 16,
    };
}
