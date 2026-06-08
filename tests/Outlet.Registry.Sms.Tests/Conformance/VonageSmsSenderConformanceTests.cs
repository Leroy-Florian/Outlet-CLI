using Microsoft.Extensions.Options;
using Outlet.Registry.Sms;
using Outlet.Registry.Sms.Tests.Support;

namespace Outlet.Registry.Sms.Tests.Conformance;

/// <summary>
/// Vonage adapter run against an in-process HTTP stub. Note Vonage answers HTTP 200 even on
/// a logical failure, so the rejecting double returns a non-zero per-message status.
/// </summary>
public sealed class VonageSmsSenderConformanceTests : SmsSenderConformanceTests
{
    protected override async Task<ISmsSenderHarness> CreateHarnessAsync(bool rejecting)
    {
        var server = rejecting
            ? new TestHttpServer(200, """{"message-count":"1","messages":[{"status":"2","error-text":"Missing api_key"}]}""")
            : new TestHttpServer(200, """{"message-count":"1","messages":[{"status":"0","message-id":"VG-0000001"}]}""");

        var sender = new VonageSmsSender(Options.Create(new VonageSmsOptions
        {
            ApiKey = "key",
            ApiSecret = "secret",
            From = "Outlet",
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
