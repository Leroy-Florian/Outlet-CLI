using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Sms;

/// <summary>DI wiring for the Amazon SNS adapter. Swap to another provider by changing this one call.</summary>
public static class AwsSnsSmsServiceCollectionExtensions
{
    public static IServiceCollection AddAwsSnsSms(this IServiceCollection services, Action<AwsSnsSmsOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<ISmsSender, AwsSnsSmsSender>();
        return services;
    }
}
