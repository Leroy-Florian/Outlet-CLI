using Microsoft.Extensions.Options;
using Outlet.Registry.Email;
using Outlet.Registry.Email.Tests.Support;

namespace Outlet.Registry.Email.Tests.Conformance;

/// <summary>SMTP adapter run against the in-process SMTP server (real protocol, hermetic).</summary>
public sealed class SmtpEmailSenderConformanceTests : EmailSenderConformanceTests
{
    protected override async Task<IEmailSenderHarness> CreateHarnessAsync(bool rejecting)
    {
        var server = new TestSmtpServer(rejecting);
        var sender = new SmtpEmailSender(Options.Create(new SmtpEmailOptions
        {
            Host = "localhost",
            Port = server.Port,
            UseStartTls = false,
        }));

        return await Task.FromResult<IEmailSenderHarness>(new Harness(server, sender));
    }

    private sealed class Harness(TestSmtpServer server, IEmailSender sender) : IEmailSenderHarness
    {
        public IEmailSender Sender => sender;
        public int DeliveredCount => server.DeliveredCount;
        public ValueTask DisposeAsync() => server.DisposeAsync();
    }
}
