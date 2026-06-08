namespace Outlet.Registry.Sms;

/// <summary>Options for the Twilio adapter, bound via <c>IOptions&lt;TwilioSmsOptions&gt;</c>.</summary>
public sealed class TwilioSmsOptions
{
    /// <summary>Twilio Account SID — the Basic-auth username.</summary>
    public string AccountSid { get; set; } = "";

    /// <summary>Twilio Auth Token — the Basic-auth password.</summary>
    public string AuthToken { get; set; } = "";

    /// <summary>Default sender: a Twilio phone number (E.164) or short code. Overridden per message by <see cref="SmsMessage.From"/>.</summary>
    public string? From { get; set; }

    /// <summary>
    /// Override the API base URL — for an outbound proxy, or pointing the adapter at a
    /// local stub in tests. Null uses Twilio's default host.
    /// </summary>
    public string? BaseUrl { get; set; }
}
