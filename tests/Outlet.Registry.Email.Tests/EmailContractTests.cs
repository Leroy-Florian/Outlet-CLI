using Outlet.Registry.Email;

namespace Outlet.Registry.Email.Tests;

public sealed class EmailContractTests
{
    [Fact]
    public void Should_DefaultCollectionsToEmpty_When_OnlyRequiredFieldsAreSet()
    {
        var message = new EmailMessage
        {
            From = new EmailAddress("noreply@acme.test"),
            To = [new EmailAddress("user@acme.test")],
            Subject = "Hello",
            TextBody = "Body",
        };

        message.Cc.Should().BeEmpty();
        message.Bcc.Should().BeEmpty();
        message.Attachments.Should().BeEmpty();
        message.HtmlBody.Should().BeNull();
    }

    [Fact]
    public void Should_CarryMessageId_When_ResultIsSuccess()
    {
        var result = EmailResult.Success("msg-123");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.MessageId.Should().Be("msg-123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Should_CarryError_When_ResultIsFailure()
    {
        var result = EmailResult.Failure("smtp down");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("smtp down");
        result.MessageId.Should().BeNull();
    }

    [Theory]
    [InlineData("user@acme.test", null, "user@acme.test")]
    [InlineData("user@acme.test", "Jane", "Jane <user@acme.test>")]
    public void Should_FormatAddress_When_DisplayNameIsOptional(string address, string? displayName, string expected)
    {
        var formatted = new EmailAddress(address, displayName).ToString();

        formatted.Should().Be(expected);
    }
}
