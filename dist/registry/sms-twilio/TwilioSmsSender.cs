using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Sms;

/// <summary>
/// Twilio adapter implementing both the generic <see cref="ISmsSender"/> and the
/// provider-specific <see cref="ITwilioSmsSender"/> from one class — registered once and
/// forwarded to both interfaces (see AddTwilioSms). Thin by design: it speaks the Twilio
/// REST API directly over <see cref="HttpClient"/> (no SDK to own), and resilience
/// (retry / circuit breaker) is composed over the port, never embedded here.
/// </summary>
public sealed class TwilioSmsSender(IOptions<TwilioSmsOptions> options) : ITwilioSmsSender
{
    private const string DefaultBaseUrl = "https://api.twilio.com";

    private readonly TwilioSmsOptions _options = options.Value;
    private readonly HttpClient _http = BuildHttpClient(options.Value);

    public Task<SmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var from = message.From ?? _options.From;
        var fields = new Dictionary<string, string>
        {
            ["To"] = message.To,
            ["Body"] = message.Body,
        };
        if (!string.IsNullOrWhiteSpace(from))
            fields["From"] = from;

        return SendCoreAsync(fields, cancellationToken);
    }

    public Task<SmsResult> SendWithMessagingServiceAsync(
        string messagingServiceSid,
        string to,
        string body,
        CancellationToken cancellationToken = default)
        => SendCoreAsync(new Dictionary<string, string>
        {
            ["To"] = to,
            ["Body"] = body,
            ["MessagingServiceSid"] = messagingServiceSid,
        }, cancellationToken);

    private async Task<SmsResult> SendCoreAsync(Dictionary<string, string> fields, CancellationToken cancellationToken)
    {
        var path = $"/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";
        try
        {
            using var content = new FormUrlEncodedContent(fields);
            using var response = await _http.PostAsync(path, content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return SmsResult.Success(ReadString(payload, "sid"));

            // Surface throttling (429) and validation errors as a typed failure — never an
            // exception for an expected delivery outcome.
            var providerMessage = ReadString(payload, "message") ?? payload;
            return SmsResult.Failure($"Twilio returned {(int)response.StatusCode}: {providerMessage}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SmsResult.Failure(ex.Message);
        }
    }

    private static HttpClient BuildHttpClient(TwilioSmsOptions options)
    {
        var http = new HttpClient { BaseAddress = new Uri(options.BaseUrl ?? DefaultBaseUrl) };

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.AccountSid}:{options.AuthToken}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return http;
    }

    private static string? ReadString(string json, string property)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
