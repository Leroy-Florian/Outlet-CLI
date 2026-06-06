using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Outlet.Registry.Email.Tests.Support;

/// <summary>
/// Minimal hand-rolled in-process SMTP server — the hermetic "level B" double (real
/// protocol, no Docker/network). Captures delivered messages; can be put in a
/// rejecting mode to provoke a clean adapter failure.
/// </summary>
public sealed class TestSmtpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<string> _messages = new();
    private readonly bool _rejecting;
    private readonly Task _acceptLoop;

    public TestSmtpServer(bool rejecting = false)
    {
        _rejecting = rejecting;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public int Port { get; }

    public int DeliveredCount => _messages.Count;

    public IReadOnlyCollection<string> Messages => [.. _messages];

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _ = Task.Run(() => HandleAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var owned = client;
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };

        await writer.WriteLineAsync("220 test-smtp ready");

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (StartsWith(line, "EHLO") || StartsWith(line, "HELO"))
            {
                await writer.WriteLineAsync("250 test-smtp");
            }
            else if (StartsWith(line, "MAIL FROM"))
            {
                await writer.WriteLineAsync(_rejecting ? "554 sender rejected" : "250 OK");
            }
            else if (StartsWith(line, "RCPT TO"))
            {
                await writer.WriteLineAsync("250 OK");
            }
            else if (StartsWith(line, "DATA"))
            {
                await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                var body = new StringBuilder();
                string? dataLine;
                while ((dataLine = await reader.ReadLineAsync(cancellationToken)) is not null && dataLine != ".")
                    body.AppendLine(dataLine);
                _messages.Enqueue(body.ToString());
                await writer.WriteLineAsync("250 OK queued");
            }
            else if (StartsWith(line, "QUIT"))
            {
                await writer.WriteLineAsync("221 Bye");
                break;
            }
            else
            {
                await writer.WriteLineAsync("250 OK");
            }
        }
    }

    private static bool StartsWith(string line, string command)
        => line.StartsWith(command, StringComparison.OrdinalIgnoreCase);

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _listener.Stop();
        try
        {
            await _acceptLoop;
        }
        catch (Exception)
        {
            // shutting down — ignore
        }
        _cts.Dispose();
    }
}
