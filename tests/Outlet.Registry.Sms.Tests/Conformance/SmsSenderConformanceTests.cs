using Outlet.Registry.Sms;

namespace Outlet.Registry.Sms.Tests.Conformance;

/// <summary>
/// A configured <see cref="ISmsSender"/> wired to its hermetic double, plus the number of
/// requests the double accepted.
/// </summary>
public interface ISmsSenderHarness : IAsyncDisposable
{
    ISmsSender Sender { get; }
    int DeliveredCount { get; }
}

/// <summary>
/// Reusable PORT conformance suite — "every ISmsSender must behave this way". Each adapter
/// runs it against its own in-process HTTP double, so swappability is TESTED, not asserted.
/// </summary>
public abstract class SmsSenderConformanceTests
{
    protected abstract Task<ISmsSenderHarness> CreateHarnessAsync(bool rejecting);

    [Fact]
    public async Task Should_ReportSuccess_And_DeliverOnce_When_ProviderAccepts()
    {
        await using var harness = await CreateHarnessAsync(rejecting: false);

        var result = await harness.Sender.SendAsync(SampleMessage());

        result.IsSuccess.Should().BeTrue(result.Error);
        harness.DeliveredCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_ReturnFailure_NotThrow_When_ProviderRejects()
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

    protected static SmsMessage SampleMessage() => new("+15550000002", "Conformance", "+15550000001");
}
