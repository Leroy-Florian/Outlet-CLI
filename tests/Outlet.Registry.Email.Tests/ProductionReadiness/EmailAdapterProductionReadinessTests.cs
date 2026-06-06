using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Outlet.Registry.Email;
using Outlet.Registry.Email.Tests.Support;

namespace Outlet.Registry.Email.Tests.ProductionReadiness;

/// <summary>
/// Hermetic production-readiness checks (HIJ-514): provoke at test time what would
/// otherwise only surface at scale — concurrency, unreachable endpoints, throttling.
/// </summary>
public sealed class EmailAdapterProductionReadinessTests
{
    [Fact]
    public async Task Smtp_Should_HandleManyConcurrentSends_WithoutLossOrCorruption()
    {
        await using var server = new TestSmtpServer();
        var sender = SmtpSender(server.Port);

        const int count = 50;
        var results = await Task.WhenAll(
            Enumerable.Range(0, count).Select(index => sender.SendAsync(Message($"msg-{index}"))));

        results.Should().OnlyContain(result => result.IsSuccess);
        server.DeliveredCount.Should().Be(count, "every concurrent send must be delivered exactly once");
    }

    [Fact]
    public async Task Smtp_Should_DegradeGracefully_When_ServerUnreachable()
    {
        var sender = SmtpSender(ClosedPort());

        var result = await sender.SendAsync(Message("unreachable"));

        result.IsFailure.Should().BeTrue("an unreachable server is a typed failure, not a hang or a throw");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendGrid_Should_SurfaceRateLimit_When_429WithRetryAfter()
    {
        await using var server = new TestHttpServer(429, ("Retry-After", "30"));

        var sender = new SendGridEmailSender(Options.Create(new SendGridEmailOptions
        {
            ApiKey = "SG.test",
            Host = server.Url,
        }));

        var result = await sender.SendAsync(Message("throttled"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("429").And.Contain("Retry-After");
    }

    private static SmtpEmailSender SmtpSender(int port)
        => new(Options.Create(new SmtpEmailOptions { Host = "localhost", Port = port, UseStartTls = false }));

    private static EmailMessage Message(string subject) => new()
    {
        From = new EmailAddress("from@outlet.test"),
        To = [new EmailAddress("to@outlet.test")],
        Subject = subject,
        TextBody = "Body",
    };

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
