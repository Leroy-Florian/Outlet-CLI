using System.Net;
using System.Net.Sockets;

namespace Outlet.Registry.Email.Tests.Support;

/// <summary>
/// Minimal hand-rolled in-process HTTP stub — the hermetic "level A" provider boundary
/// for the SendGrid SDK (no Docker, no external network, no extra dependency). Replies
/// to every request with a fixed status + headers and counts the requests it received.
/// </summary>
public sealed class TestHttpServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly int _statusCode;
    private readonly (string Name, string Value)[] _headers;
    private readonly Task _loop;
    private int _requestCount;

    public TestHttpServer(int statusCode, params (string Name, string Value)[] headers)
    {
        _statusCode = statusCode;
        _headers = headers;

        var port = FreePort();
        Url = $"http://localhost:{port}";
        _listener.Prefixes.Add(Url + "/");
        _listener.Start();
        _loop = Task.Run(LoopAsync);
    }

    public string Url { get; }

    public int RequestCount => Volatile.Read(ref _requestCount);

    private async Task LoopAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                break;
            }

            Interlocked.Increment(ref _requestCount);

            await using (var input = context.Request.InputStream)
            {
                await input.CopyToAsync(Stream.Null);
            }

            context.Response.StatusCode = _statusCode;
            foreach (var (name, value) in _headers)
                context.Response.AddHeader(name, value);
            context.Response.Close();
        }
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        _listener.Stop();
        _listener.Close();
        try
        {
            await _loop;
        }
        catch (Exception)
        {
            // shutting down — ignore
        }
    }
}
