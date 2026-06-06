namespace Outlet.Registry.Email;

/// <summary>
/// Generic e-mail message — the common ~80% case (no god-model). Recipients and
/// attachments default to empty; supply a text body, an HTML body, or both.
/// </summary>
public sealed class EmailMessage
{
    public required EmailAddress From { get; init; }
    public required IReadOnlyList<EmailAddress> To { get; init; }
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];
    public IReadOnlyList<EmailAddress> Bcc { get; init; } = [];
    public required string Subject { get; init; }
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];
}
