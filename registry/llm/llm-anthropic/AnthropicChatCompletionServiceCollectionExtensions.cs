using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Llm;

/// <summary>DI wiring for the Anthropic adapter. Swap to another provider by changing this one call.</summary>
public static class AnthropicChatCompletionServiceCollectionExtensions
{
    public static IServiceCollection AddAnthropicChatCompletion(this IServiceCollection services, Action<AnthropicChatCompletionOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IChatCompletion, AnthropicChatCompletion>();
        return services;
    }
}
