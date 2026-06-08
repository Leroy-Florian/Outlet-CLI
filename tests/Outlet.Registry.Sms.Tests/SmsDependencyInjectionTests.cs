using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Sms;

namespace Outlet.Registry.Sms.Tests;

public sealed class SmsDependencyInjectionTests
{
    [Fact]
    public void Should_ResolveVonageSender_When_AddVonageSmsIsCalled()
    {
        var services = new ServiceCollection();
        services.AddVonageSms(o =>
        {
            o.ApiKey = "key";
            o.ApiSecret = "secret";
        });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISmsSender>();

        sender.Should().BeOfType<VonageSmsSender>();
    }

    [Fact]
    public void Should_ResolveAwsSnsSender_When_AddAwsSnsSmsIsCalled()
    {
        var services = new ServiceCollection();
        services.AddAwsSnsSms(o =>
        {
            o.AccessKeyId = "id";
            o.SecretAccessKey = "secret";
        });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISmsSender>();

        sender.Should().BeOfType<AwsSnsSmsSender>();
    }

    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddTwilioSmsIsCalled()
    {
        var services = new ServiceCollection();
        services.AddTwilioSms(o =>
        {
            o.AccountSid = "AC-test";
            o.AuthToken = "token";
        });

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<ISmsSender>();
        var specific = provider.GetRequiredService<ITwilioSmsSender>();

        generic.Should().BeOfType<TwilioSmsSender>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        ISmsSender Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider().GetRequiredService<ISmsSender>();
        }

        var twilio = Resolve(s => s.AddTwilioSms(o => { o.AccountSid = "AC"; o.AuthToken = "t"; }));
        var vonage = Resolve(s => s.AddVonageSms(o => { o.ApiKey = "k"; o.ApiSecret = "s"; }));
        var awsSns = Resolve(s => s.AddAwsSnsSms(o => { o.AccessKeyId = "id"; o.SecretAccessKey = "s"; }));

        twilio.Should().BeOfType<TwilioSmsSender>();
        vonage.Should().BeOfType<VonageSmsSender>();
        awsSns.Should().BeOfType<AwsSnsSmsSender>();
    }
}
