using Outlet.Registry.Email;

namespace Outlet.Registry.Email.Tests.Conformance;

/// <summary>
/// A configured <see cref="IEmailSender"/> wired to its hermetic double, plus the
/// number of messages the double accepted.
/// </summary>
public interface IEmailSenderHarness : IAsyncDisposable
{
    IEmailSender Sender { get; }
    int DeliveredCount { get; }
}

/// <summary>
/// Reusable PORT conformance suite — "every IEmailSender must behave this way".
/// Each adapter runs it against its own double (SMTP in-process server / SendGrid
/// HTTP stub), so swappability is TESTED, not asserted.
/// </summary>
public abstract class EmailSenderConformanceTests
{
    protected abstract Task<IEmailSenderHarness> CreateHarnessAsync(bool rejecting);

    [Fact]
    public async Task Should_ReportSuccess_And_DeliverOnce_When_ServerAccepts()
    {
        await using var harness = await CreateHarnessAsync(rejecting: false);

        var result = await harness.Sender.SendAsync(SampleMessage());

        result.IsSuccess.Should().BeTrue(result.Error);
        harness.DeliveredCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnFailure_NotThrow_When_ServerRejects()
    {
        await using var harness = await CreateHarnessAsync(rejecting: true);

        var result = await harness.Sender.SendAsync(SampleMessage());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_HonorCancellation()
    {
        await using var harness = await CreateHarnessAsync(rejecting: false);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await harness.Sender.SendAsync(SampleMessage(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    protected static EmailMessage SampleMessage() => new()
    {
        From = new EmailAddress("from@outlet.test", "Outlet"),
        To = [new EmailAddress("to@outlet.test")],
        Subject = "Conformance",
        TextBody = "Body",
    };
}
