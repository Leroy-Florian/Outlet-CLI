namespace Outlet.Registry.Sms;

/// <summary>
/// Provider-specific companion to <see cref="ISmsSender"/>. It lives BESIDE the generic
/// port (never inside it) and is implemented by the very same adapter instance, so generic
/// code stays swappable while Twilio-only features (sending through a Messaging Service,
/// which enables sender pools / sticky sender / scaling) remain reachable when you opt in.
/// </summary>
public interface ITwilioSmsSender : ISmsSender
{
    /// <summary>Sends an SMS through a Twilio Messaging Service SID rather than an explicit sender number.</summary>
    Task<SmsResult> SendWithMessagingServiceAsync(
        string messagingServiceSid,
        string to,
        string body,
        CancellationToken cancellationToken = default);
}
