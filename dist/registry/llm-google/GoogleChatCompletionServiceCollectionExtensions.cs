using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Llm;

/// <summary>DI wiring for the Google Gemini adapter. Swap to another provider by changing this one call.</summary>
public static class GoogleChatCompletionServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleChatCompletion(this IServiceCollection services, Action<GoogleChatCompletionOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IChatCompletion, GoogleChatCompletion>();
        return services;
    }
}
