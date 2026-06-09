using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Sms;

/// <summary>
/// Vonage (formerly Nexmo) adapter for <see cref="ISmsSender"/>, speaking the SMS REST API
/// directly over <see cref="HttpClient"/> (no SDK to own). Thin by design: resilience
/// (retry / circuit breaker) is composed over the port, never embedded here.
/// </summary>
public sealed class VonageSmsSender(IOptions<VonageSmsOptions> options) : ISmsSender
{
    private const string DefaultBaseUrl = "https://rest.nexmo.com";

    private readonly VonageSmsOptions _options = options.Value;
    private readonly HttpClient _http = new() { BaseAddress = new Uri(options.Value.BaseUrl ?? DefaultBaseUrl) };

    public async Task<SmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["api_key"] = _options.ApiKey,
            ["api_secret"] = _options.ApiSecret,
            ["to"] = message.To,
            ["from"] = message.From ?? _options.From ?? "",
            ["text"] = message.Body,
        };

        try
        {
            using var content = new FormUrlEncodedContent(fields);
            using var response = await _http.PostAsync("/sms/json", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return SmsResult.Failure($"Vonage returned {(int)response.StatusCode}: {payload}");

            return ParseResult(payload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SmsResult.Failure(ex.Message);
        }
    }

    // Vonage answers HTTP 200 even for logical failures: the real outcome is the per-message
    // "status" field ("0" == accepted by the carrier), so we branch on the body, not the code.
    private static SmsResult ParseResult(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("messages", out var messages)
                || messages.ValueKind != JsonValueKind.Array
                || messages.GetArrayLength() == 0)
                return SmsResult.Failure($"Vonage returned an unexpected response: {payload}");

            var first = messages[0];
            var status = first.TryGetProperty("status", out var statusValue) ? statusValue.GetString() : null;

            if (status == "0")
            {
                var messageId = first.TryGetProperty("message-id", out var id) ? id.GetString() : null;
                return SmsResult.Success(messageId);
            }

            var error = first.TryGetProperty("error-text", out var errorText) ? errorText.GetString() : "unknown error";
            return SmsResult.Failure($"Vonage status {status}: {error}");
        }
        catch (JsonException)
        {
            return SmsResult.Failure($"Vonage returned malformed JSON: {payload}");
        }
    }
}
