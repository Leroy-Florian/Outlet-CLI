using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Sms;

/// <summary>DI wiring for the Twilio adapter. Swap to another provider by changing this one call.</summary>
public static class TwilioSmsServiceCollectionExtensions
{
    public static IServiceCollection AddTwilioSms(this IServiceCollection services, Action<TwilioSmsOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<TwilioSmsSender>();
        services.AddSingleton<ISmsSender>(sp => sp.GetRequiredService<TwilioSmsSender>());
        services.AddSingleton<ITwilioSmsSender>(sp => sp.GetRequiredService<TwilioSmsSender>());

        return services;
    }
}
