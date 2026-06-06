namespace Outlet.Registry.Email;

/// <summary>
/// Provider-specific companion to <see cref="IEmailSender"/>. It lives BESIDE the
/// generic port (never inside it) and is implemented by the very same adapter
/// instance, so generic code stays swappable while SendGrid-only features
/// (dynamic templates) remain reachable when you opt in.
/// </summary>
public interface ISendGridEmailSender : IEmailSender
{
    /// <summary>Sends a SendGrid dynamic-template e-mail with the given template data.</summary>
    Task<EmailResult> SendTemplateAsync(
        EmailAddress from,
        EmailAddress to,
        string templateId,
        object templateData,
        CancellationToken cancellationToken = default);
}
