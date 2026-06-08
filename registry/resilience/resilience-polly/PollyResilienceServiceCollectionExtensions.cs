using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Resilience;

/// <summary>DI wiring for the Polly adapter. Swap to another provider by changing this one call.</summary>
public static class PollyResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddPollyResilience(this IServiceCollection services, Action<ResilienceOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<PollyResilienceExecutor>();
        services.AddSingleton<IResilienceExecutor>(sp => sp.GetRequiredService<PollyResilienceExecutor>());
        services.AddSingleton<IPollyResilienceExecutor>(sp => sp.GetRequiredService<PollyResilienceExecutor>());

        return services;
    }
}
