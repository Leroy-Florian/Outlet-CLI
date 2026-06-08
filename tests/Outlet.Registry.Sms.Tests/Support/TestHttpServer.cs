using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Outlet.Registry.Sms.Tests.Support;

/// <summary>
/// Minimal hand-rolled in-process HTTP stub — the hermetic "level A" provider boundary for
/// the SMS REST adapters (no Docker, no external network, no extra dependency). Replies to
/// every request with a fixed status + body, drains and records request bodies, and counts
/// the requests it received. Each request is handled on its own task so the stub stays
/// responsive under concurrency.
/// </summary>
public sealed class TestHttpServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly int _statusCode;
    private readonly string _body;
    private readonly string _contentType;
    private readonly (string Name, string Value)[] _headers;
    private readonly Task _loop;
    private int _requestCount;

    public TestHttpServer(
        int statusCode,
        string body = "",
        string contentType = "application/json",
        params (string Name, string Value)[] headers)
    {
        _statusCode = statusCode;
        _body = body;
        _contentType = contentType;
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
            _ = Task.Run(() => RespondAsync(context));
        }
    }

    private async Task RespondAsync(HttpListenerContext context)
    {
        try
        {
            await using (var input = context.Request.InputStream)
            {
                await input.CopyToAsync(Stream.Null);
            }

            var payload = Encoding.UTF8.GetBytes(_body);
            context.Response.StatusCode = _statusCode;
            context.Response.ContentType = _contentType;
            context.Response.ContentLength64 = payload.Length;
            foreach (var (name, value) in _headers)
                context.Response.AddHeader(name, value);

            await context.Response.OutputStream.WriteAsync(payload);
            context.Response.Close();
        }
        catch (Exception)
        {
            // client went away / server shutting down — ignore
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
