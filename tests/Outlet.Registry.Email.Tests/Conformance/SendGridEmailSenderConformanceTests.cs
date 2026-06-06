using Microsoft.Extensions.Options;
using Outlet.Registry.Email;
using Outlet.Registry.Email.Tests.Support;

namespace Outlet.Registry.Email.Tests.Conformance;

/// <summary>SendGrid adapter run against an in-process HTTP stub (level A — provider boundary mocked).</summary>
public sealed class SendGridEmailSenderConformanceTests : EmailSenderConformanceTests
{
    protected override async Task<IEmailSenderHarness> CreateHarnessAsync(bool rejecting)
    {
        var server = rejecting
            ? new TestHttpServer(500)
            : new TestHttpServer(202, ("X-Message-Id", "test-message-id"));

        var sender = new SendGridEmailSender(Options.Create(new SendGridEmailOptions
        {
            ApiKey = "SG.test-key",
            Host = server.Url,
        }));

        return await Task.FromResult<IEmailSenderHarness>(new Harness(server, sender));
    }

    private sealed class Harness(TestHttpServer server, IEmailSender sender) : IEmailSenderHarness
    {
        public IEmailSender Sender => sender;
        public int DeliveredCount => server.RequestCount;
        public ValueTask DisposeAsync() => server.DisposeAsync();
    }
}
