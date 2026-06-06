using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Email;

namespace Outlet.Registry.Email.Tests;

public sealed class EmailDependencyInjectionTests
{
    [Fact]
    public void Should_ResolveSmtpSender_When_AddSmtpEmailIsCalled()
    {
        var services = new ServiceCollection();
        services.AddSmtpEmail(o =>
        {
            o.Host = "smtp.acme.test";
            o.Port = 587;
        });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetService<IEmailSender>();

        sender.Should().BeOfType<SmtpEmailSender>();
    }

    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddSendGridEmailIsCalled()
    {
        var services = new ServiceCollection();
        services.AddSendGridEmail(o => o.ApiKey = "SG.test");

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<IEmailSender>();
        var specific = provider.GetRequiredService<ISendGridEmailSender>();

        generic.Should().BeOfType<SendGridEmailSender>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        IEmailSender Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider().GetRequiredService<IEmailSender>();
        }

        var smtp = Resolve(s => s.AddSmtpEmail(o => o.Host = "smtp.acme.test"));
        var sendGrid = Resolve(s => s.AddSendGridEmail(o => o.ApiKey = "SG.test"));

        smtp.Should().BeOfType<SmtpEmailSender>();
        sendGrid.Should().BeOfType<SendGridEmailSender>();
    }
}
