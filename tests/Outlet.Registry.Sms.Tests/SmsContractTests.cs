using Outlet.Registry.Sms;

namespace Outlet.Registry.Sms.Tests;

public sealed class SmsContractTests
{
    [Fact]
    public void Should_DefaultFromToNull_When_OnlyRequiredFieldsAreSet()
    {
        var message = new SmsMessage("+15550000002", "Hello");

        message.To.Should().Be("+15550000002");
        message.Body.Should().Be("Hello");
        message.From.Should().BeNull();
    }

    [Fact]
    public void Should_CarryMessageId_When_ResultIsSuccess()
    {
        var result = SmsResult.Success("msg-123");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.MessageId.Should().Be("msg-123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Should_CarryError_When_ResultIsFailure()
    {
        var result = SmsResult.Failure("carrier rejected");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("carrier rejected");
        result.MessageId.Should().BeNull();
    }
}
