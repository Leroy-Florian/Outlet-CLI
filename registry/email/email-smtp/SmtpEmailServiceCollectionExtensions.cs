using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Email;

/// <summary>DI wiring for the SMTP adapter. Swap to another provider by changing this one call.</summary>
public static class SmtpEmailServiceCollectionExtensions
{
    public static IServiceCollection AddSmtpEmail(this IServiceCollection services, Action<SmtpEmailOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        return services;
    }
}
