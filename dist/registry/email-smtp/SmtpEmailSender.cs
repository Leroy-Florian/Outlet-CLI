using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Outlet.Registry.Email;

/// <summary>
/// SMTP adapter for <see cref="IEmailSender"/>, backed by MailKit. Thin by design:
/// resilience (retry / circuit breaker) is composed over the port, never embedded here.
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options) : IEmailSender
{
    private readonly SmtpEmailOptions _options = options.Value;

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mime = BuildMimeMessage(message);

        using var client = new SmtpClient();
        try
        {
            var secureOptions = _options.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_options.Host, _options.Port, secureOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);

            var response = await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            return EmailResult.Success(string.IsNullOrWhiteSpace(response) ? null : response);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EmailResult.Failure(ex.Message);
        }
    }

    private static MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(ToMailbox(message.From));
        mime.To.AddRange(message.To.Select(ToMailbox));
        mime.Cc.AddRange(message.Cc.Select(ToMailbox));
        mime.Bcc.AddRange(message.Bcc.Select(ToMailbox));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
        };

        foreach (var attachment in message.Attachments)
            builder.Attachments.Add(attachment.FileName, attachment.Content.ToArray(), ContentType.Parse(attachment.ContentType));

        mime.Body = builder.ToMessageBody();
        return mime;
    }

    private static MailboxAddress ToMailbox(EmailAddress address)
        => new(address.DisplayName ?? string.Empty, address.Address);
}
