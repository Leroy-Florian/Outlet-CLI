using System.Net;

namespace Outlet.Core.Infrastructure.UnitTests.Fakes;

/// <summary>
/// Hand-written <see cref="HttpMessageHandler"/> mapping absolute URLs to canned
/// responses — the level-A (no-network) HTTP boundary. No mocking framework.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Content)> _responses = [];

    public StubHttpMessageHandler Map(string url, string content, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses[url] = (status, content);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.AbsoluteUri;

        if (_responses.TryGetValue(url, out var mapped))
            return Task.FromResult(new HttpResponseMessage(mapped.Status)
            {
                Content = new StringContent(mapped.Content),
            });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
