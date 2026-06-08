using System.Net;

namespace Outlet.Registry.Llm.Tests.Support;

/// <summary>
/// Hermetic "level A" provider boundary for the LLM SDKs — no Docker, no network. Returns a
/// canned body for a normal completion and a separate canned body for a streaming completion
/// (detected from the request), counts the requests it saw, and can be flipped to reject with
/// a 5xx so adapters can be proven to surface a typed failure instead of throwing.
/// </summary>
public sealed class StubHttpMessageHandler(
    string completionBody,
    string streamBody,
    string streamContentType = "text/event-stream",
    bool rejecting = false) : HttpMessageHandler
{
    private int _requestCount;

    public int RequestCount => Volatile.Read(ref _requestCount);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);

        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        if (rejecting)
            return Respond(HttpStatusCode.InternalServerError, "{\"error\":{\"message\":\"stub rejected the request\"}}", "application/json");

        return IsStreaming(request, body)
            ? Respond(HttpStatusCode.OK, streamBody, streamContentType)
            : Respond(HttpStatusCode.OK, completionBody, "application/json");
    }

    private static bool IsStreaming(HttpRequestMessage request, string body)
    {
        var uri = request.RequestUri?.ToString() ?? string.Empty;
        if (uri.Contains("stream", StringComparison.OrdinalIgnoreCase))
            return true;
        if (request.Headers.Accept.Any(a => a.MediaType == "text/event-stream"))
            return true;
        return body.Contains("\"stream\":true", StringComparison.Ordinal)
            || body.Contains("\"stream\": true", StringComparison.Ordinal);
    }

    private static HttpResponseMessage Respond(HttpStatusCode status, string body, string contentType) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, contentType) };
}
