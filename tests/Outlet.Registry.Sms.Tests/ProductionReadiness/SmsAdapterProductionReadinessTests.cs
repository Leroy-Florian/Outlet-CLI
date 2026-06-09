using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Outlet.Registry.Sms;
using Outlet.Registry.Sms.Tests.Support;

namespace Outlet.Registry.Sms.Tests.ProductionReadiness;

/// <summary>
/// Hermetic production-readiness checks: provoke at test time what would otherwise only
/// surface at scale — concurrency, unreachable endpoints, throttling, and providers that
/// report logical failure inside an HTTP 200.
/// </summary>
public sealed class SmsAdapterProductionReadinessTests
{
    [Fact]
    public async Task Twilio_Should_HandleManyConcurrentSends_WithoutLossOrCorruption()
    {
        await using var server = new TestHttpServer(201, """{"sid":"SM00000000000000000000000000000001","status":"queued"}""");
        var sender = TwilioSender(server.Url);

        const int count = 50;
        var results = await Task.WhenAll(
            Enumerable.Range(0, count).Select(index => sender.SendAsync(Message($"msg-{index}"))));

        results.Should().OnlyContain(result => result.IsSuccess);
        server.RequestCount.Should().Be(count, "every concurrent send must reach the provider exactly once");
    }

    [Fact]
    public async Task Twilio_Should_DegradeGracefully_When_EndpointUnreachable()
    {
        var sender = TwilioSender($"http://localhost:{ClosedPort()}");

        var result = await sender.SendAsync(Message("unreachable"));

        result.IsFailure.Should().BeTrue("an unreachable endpoint is a typed failure, not a hang or a throw");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Twilio_Should_SurfaceRateLimit_When_429()
    {
        await using var server = new TestHttpServer(429, """{"code":20429,"message":"Too Many Requests"}""");
        var sender = TwilioSender(server.Url);

        var result = await sender.SendAsync(Message("throttled"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("429");
    }

    [Fact]
    public async Task Twilio_Should_SendViaMessagingService_When_CompanionFeatureUsed()
    {
        await using var server = new TestHttpServer(201, """{"sid":"SM00000000000000000000000000000002","status":"queued"}""");
        var sender = TwilioSender(server.Url);

        var result = await sender.SendWithMessagingServiceAsync("MG-test", "+15550000002", "Via messaging service");

        result.IsSuccess.Should().BeTrue(result.Error);
        result.MessageId.Should().Be("SM00000000000000000000000000000002");
    }

    [Fact]
    public async Task Vonage_Should_SurfaceLogicalFailure_When_StatusIsNotZero_DespiteHttp200()
    {
        await using var server = new TestHttpServer(200, """{"message-count":"1","messages":[{"status":"2","error-text":"Missing api_key"}]}""");
        var sender = new VonageSmsSender(Options.Create(new VonageSmsOptions
        {
            ApiKey = "key",
            ApiSecret = "secret",
            BaseUrl = server.Url,
        }));

        var result = await sender.SendAsync(Message("logical-failure"));

        result.IsFailure.Should().BeTrue("HTTP 200 with a non-zero status is still a delivery failure");
        result.Error.Should().Contain("Missing api_key");
    }

    private static TwilioSmsSender TwilioSender(string baseUrl)
        => new(Options.Create(new TwilioSmsOptions
        {
            AccountSid = "AC-test",
            AuthToken = "token",
            From = "+15550000001",
            BaseUrl = baseUrl,
        }));

    private static SmsMessage Message(string body) => new("+15550000002", body, "+15550000001");

    // A port that nothing listens on: bind to an ephemeral port, then release it.
    private static int ClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
