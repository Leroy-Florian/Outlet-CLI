using Microsoft.Extensions.Options;
using SendGrid;
using SgMail = SendGrid.Helpers.Mail;

namespace Outlet.Registry.Email;

/// <summary>
/// SendGrid adapter implementing both the generic <see cref="IEmailSender"/> and the
/// provider-specific <see cref="ISendGridEmailSender"/> from one class — registered
/// once and forwarded to both interfaces (see AddSendGridEmail).
/// </summary>
public sealed class SendGridEmailSender(IOptions<SendGridEmailOptions> options) : ISendGridEmailSender
{
    private readonly SendGridEmailOptions _options = options.Value;

    public Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => SendCoreAsync(BuildMessage(message), cancellationToken);

    public Task<EmailResult> SendTemplateAsync(
        EmailAddress from,
        EmailAddress to,
        string templateId,
        object templateData,
        CancellationToken cancellationToken = default)
    {
        var message = SgMail.MailHelper.CreateSingleTemplateEmail(
            ToSendGrid(from), ToSendGrid(to), templateId, templateData);

        return SendCoreAsync(message, cancellationToken);
    }

    private async Task<EmailResult> SendCoreAsync(SgMail.SendGridMessage message, CancellationToken cancellationToken)
    {
        var client = new SendGridClient(_options.ApiKey);
        try
        {
            var response = await client.SendEmailAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers.TryGetValues("X-Message-Id", out var ids)
                    ? ids.FirstOrDefault()
                    : null;
                return EmailResult.Success(messageId);
            }

            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            return EmailResult.Failure($"SendGrid returned {(int)response.StatusCode}: {body}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EmailResult.Failure(ex.Message);
        }
    }

    private static SgMail.SendGridMessage BuildMessage(EmailMessage message)
    {
        var sg = new SgMail.SendGridMessage();
        sg.SetFrom(ToSendGrid(message.From));
        sg.AddTos([.. message.To.Select(ToSendGrid)]);
        sg.SetSubject(message.Subject);

        if (message.Cc.Count > 0)
            sg.AddCcs([.. message.Cc.Select(ToSendGrid)]);
        if (message.Bcc.Count > 0)
            sg.AddBccs([.. message.Bcc.Select(ToSendGrid)]);

        if (!string.IsNullOrEmpty(message.TextBody))
            sg.AddContent("text/plain", message.TextBody);
        if (!string.IsNullOrEmpty(message.HtmlBody))
            sg.AddContent("text/html", message.HtmlBody);

        foreach (var attachment in message.Attachments)
            sg.AddAttachment(
                attachment.FileName,
                Convert.ToBase64String(attachment.Content.ToArray()),
                attachment.ContentType);

        return sg;
    }

    private static SgMail.EmailAddress ToSendGrid(EmailAddress address)
        => new(address.Address, address.DisplayName);
}
