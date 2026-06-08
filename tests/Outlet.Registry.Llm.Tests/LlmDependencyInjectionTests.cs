using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Llm;

namespace Outlet.Registry.Llm.Tests;

public sealed class LlmDependencyInjectionTests
{
    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddOpenAiChatCompletionIsCalled()
    {
        var services = new ServiceCollection();
        services.AddOpenAiChatCompletion(o => o.ApiKey = "sk-test");

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<IChatCompletion>();
        var specific = provider.GetRequiredService<IOpenAiChatCompletion>();

        generic.Should().BeOfType<OpenAiChatCompletion>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_ResolveAnthropicSender_When_AddAnthropicChatCompletionIsCalled()
    {
        var services = new ServiceCollection();
        services.AddAnthropicChatCompletion(o => o.ApiKey = "sk-ant-test");

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IChatCompletion>().Should().BeOfType<AnthropicChatCompletion>();
    }

    [Fact]
    public void Should_ResolveGoogleSender_When_AddGoogleChatCompletionIsCalled()
    {
        var services = new ServiceCollection();
        services.AddGoogleChatCompletion(o => o.ApiKey = "test-key");

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IChatCompletion>().Should().BeOfType<GoogleChatCompletion>();
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        IChatCompletion Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider().GetRequiredService<IChatCompletion>();
        }

        Resolve(s => s.AddOpenAiChatCompletion(o => o.ApiKey = "sk-test")).Should().BeOfType<OpenAiChatCompletion>();
        Resolve(s => s.AddAnthropicChatCompletion(o => o.ApiKey = "sk-ant-test")).Should().BeOfType<AnthropicChatCompletion>();
        Resolve(s => s.AddGoogleChatCompletion(o => o.ApiKey = "test-key")).Should().BeOfType<GoogleChatCompletion>();
    }
}
