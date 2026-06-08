namespace Outlet.Registry.Sms;

/// <summary>
/// Generic SMS message — the common ~80% case (no god-model). A destination number in
/// E.164 form (e.g. "+15551234567"), the text body, and an optional sender: a phone
/// number or, where the provider and destination country allow it, an alphanumeric id.
/// Need MMS, scheduling or per-provider knobs? Edit your owned copy — that is the point.
/// </summary>
public sealed record SmsMessage(string To, string Body, string? From = null);
