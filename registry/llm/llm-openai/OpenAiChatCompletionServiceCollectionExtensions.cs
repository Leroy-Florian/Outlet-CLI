using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Llm;

/// <summary>DI wiring for the OpenAI adapter. Swap to another provider by changing this one call.</summary>
public static class OpenAiChatCompletionServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiChatCompletion(this IServiceCollection services, Action<OpenAiChatCompletionOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<OpenAiChatCompletion>();
        services.AddSingleton<IChatCompletion>(sp => sp.GetRequiredService<OpenAiChatCompletion>());
        services.AddSingleton<IOpenAiChatCompletion>(sp => sp.GetRequiredService<OpenAiChatCompletion>());

        return services;
    }
}
