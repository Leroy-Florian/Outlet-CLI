using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Resilience;

/// <summary>DI wiring for the zero-dependency built-in adapter. Swap to another provider by changing this one call.</summary>
public static class BuiltInResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddBuiltInResilience(this IServiceCollection services, Action<ResilienceOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IResilienceExecutor, BuiltInResilienceExecutor>();
        return services;
    }
}
