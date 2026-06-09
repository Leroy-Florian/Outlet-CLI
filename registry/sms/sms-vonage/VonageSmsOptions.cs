namespace Outlet.Registry.Sms;

/// <summary>Options for the Vonage adapter, bound via <c>IOptions&lt;VonageSmsOptions&gt;</c>.</summary>
public sealed class VonageSmsOptions
{
    /// <summary>Vonage API key.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Vonage API secret.</summary>
    public string ApiSecret { get; set; } = "";

    /// <summary>Default sender: an E.164 number or, where allowed, an alphanumeric id. Overridden per message by <see cref="SmsMessage.From"/>.</summary>
    public string? From { get; set; }

    /// <summary>
    /// Override the API base URL — for an outbound proxy, or pointing the adapter at a
    /// local stub in tests. Null uses Vonage's default host.
    /// </summary>
    public string? BaseUrl { get; set; }
}
