namespace Outlet.Registry.Email;

/// <summary>
/// Generic e-mail port — identical across every adapter so providers stay swappable.
/// No provider specifics ever leak into this interface; a provider-specific feature
/// goes on a dedicated companion interface implemented by the same adapter.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends <paramref name="message"/>, returning a success/failure outcome (never throws for expected delivery errors).</summary>
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
