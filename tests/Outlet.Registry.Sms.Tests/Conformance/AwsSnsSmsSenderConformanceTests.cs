using Microsoft.Extensions.Options;
using Outlet.Registry.Sms;
using Outlet.Registry.Sms.Tests.Support;

namespace Outlet.Registry.Sms.Tests.Conformance;

/// <summary>
/// Amazon SNS adapter run against an in-process HTTP stub returning canned SNS XML
/// (level A — provider boundary mocked; the SDK signs the request, the stub ignores it).
/// </summary>
public sealed class AwsSnsSmsSenderConformanceTests : SmsSenderConformanceTests
{
    protected override async Task<ISmsSenderHarness> CreateHarnessAsync(bool rejecting)
    {
        var server = rejecting
            ? new TestHttpServer(400, SnsResponses.Error("InvalidParameter", "Invalid parameter: PhoneNumber"), "text/xml")
            : new TestHttpServer(200, SnsResponses.PublishSuccess("00000000-0000-0000-0000-000000000001"), "text/xml");

        var sender = new AwsSnsSmsSender(Options.Create(new AwsSnsSmsOptions
        {
            AccessKeyId = "test",
            SecretAccessKey = "test",
            Region = "us-east-1",
            ServiceUrl = server.Url,
        }));

        return await Task.FromResult<ISmsSenderHarness>(new Harness(server, sender));
    }

    private sealed class Harness(TestHttpServer server, AwsSnsSmsSender sender) : ISmsSenderHarness
    {
        public ISmsSender Sender => sender;
        public int DeliveredCount => server.RequestCount;

        public ValueTask DisposeAsync()
        {
            sender.Dispose();
            return server.DisposeAsync();
        }
    }
}
