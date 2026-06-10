using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Outlet.E2E.Tests.Support;

/// <summary>
/// Serves <c>dist/registry/</c> over loopback HTTP, in the exact layout the CLI's
/// <c>HttpRegistrySource</c> expects (<c>/registry.json</c> index +
/// <c>/{item}/{file}</c>). This is what makes the E2E suite hermetic: the real CLI
/// fetches items from here instead of the public registry — no Docker, no internet,
/// and identical on Linux, macOS and Windows (Kestrel on 127.0.0.1, no URL ACLs).
/// </summary>
public sealed class LocalRegistryServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private LocalRegistryServer(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    /// <summary>Absolute base URL with a trailing slash — drop it into outlet.json's registry url.</summary>
    public string BaseUrl { get; }

    public static async Task<LocalRegistryServer> StartAsync(string distRoot)
    {
        var root = Path.GetFullPath(distRoot);

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.MapGet("/{**path}", (string? path) =>
        {
            var relative = string.IsNullOrEmpty(path) ? "registry.json" : path;
            var full = Path.GetFullPath(Path.Combine(root, relative));

            // Never serve outside the registry root, and 404 like the HTTP source expects.
            if (!full.StartsWith(root, StringComparison.Ordinal) || !File.Exists(full))
                return Results.NotFound();

            return Results.Text(File.ReadAllText(full), "text/plain");
        });

        await app.StartAsync();

        var address = app.Urls.First();
        var baseUrl = address.EndsWith('/') ? address : address + "/";
        return new LocalRegistryServer(app, baseUrl);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
