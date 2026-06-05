using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Email;

/// <summary>DI wiring for the SendGrid adapter. Swap to another provider by changing this one call.</summary>
public static class SendGridEmailServiceCollectionExtensions
{
    public static IServiceCollection AddSendGridEmail(this IServiceCollection services, Action<SendGridEmailOptions> configure)
    {
        services.Configure(configure);

        // ONE concrete instance, forwarded to both the generic and the provider-specific port.
        services.AddSingleton<SendGridEmailSender>();
        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<SendGridEmailSender>());
        services.AddSingleton<ISendGridEmailSender>(sp => sp.GetRequiredService<SendGridEmailSender>());

        return services;
    }
}
