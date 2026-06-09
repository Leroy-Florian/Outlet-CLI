using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Sms;

/// <summary>DI wiring for the Vonage adapter. Swap to another provider by changing this one call.</summary>
public static class VonageSmsServiceCollectionExtensions
{
    public static IServiceCollection AddVonageSms(this IServiceCollection services, Action<VonageSmsOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<ISmsSender, VonageSmsSender>();
        return services;
    }
}
