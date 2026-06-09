using Microsoft.Extensions.Options;
using Outlet.Registry.Sms;
using Outlet.Registry.Sms.Tests.Support;

namespace Outlet.Registry.Sms.Tests.Conformance;

/// <summary>Twilio adapter run against an in-process HTTP stub (level A — provider boundary mocked).</summary>
public sealed class TwilioSmsSenderConformanceTests : SmsSenderConformanceTests
{
    protected override async Task<ISmsSenderHarness> CreateHarnessAsync(bool rejecting)
    {
        var server = rejecting
            ? new TestHttpServer(400, """{"code":21211,"message":"Invalid 'To' Phone Number"}""")
            : new TestHttpServer(201, """{"sid":"SM00000000000000000000000000000001","status":"queued"}""");

        var sender = new TwilioSmsSender(Options.Create(new TwilioSmsOptions
        {
            AccountSid = "AC-test",
            AuthToken = "test-token",
            From = "+15550000001",
            BaseUrl = server.Url,
        }));

        return await Task.FromResult<ISmsSenderHarness>(new Harness(server, sender));
    }

    private sealed class Harness(TestHttpServer server, ISmsSender sender) : ISmsSenderHarness
    {
        public ISmsSender Sender => sender;
        public int DeliveredCount => server.RequestCount;
        public ValueTask DisposeAsync() => server.DisposeAsync();
    }
}
