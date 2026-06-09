using System.Net;
using System.Text.Json;
using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.Cli;

namespace Outlet.Core.Infrastructure.Cli;

/// <summary>
/// SECONDARY ADAPTER — reads the latest published CLI version from a NuGet
/// "flat container" feed: <c>{baseUri}/{package-id-lowercase}/index.json</c>
/// returns <c>{ "versions": [ "0.1.0", … ] }</c>.
///
/// Best-effort by design (see <see cref="ICliReleaseClient"/>): any network or
/// parsing problem yields null so the background check stays invisible offline.
/// A short timeout keeps it from ever stalling a command.
/// </summary>
public sealed class HttpNuGetCliReleaseClient(HttpClient httpClient, Uri flatContainerBaseUri, string packageId)
    : ICliReleaseClient
{
    private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(3);

    public async Task<CliVersion?> GetLatestStableVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var indexUri = new Uri(
                EnsureTrailingSlash(flatContainerBaseUri),
                $"{packageId.ToLowerInvariant()}/index.json");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(LookupTimeout);

            using var response = await httpClient.GetAsync(indexUri, timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            return PickLatestStable(stream);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            // Unknown latest version — the check simply does nothing this time.
            return null;
        }
    }

    private static CliVersion? PickLatestStable(Stream json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("versions", out var versions)
            || versions.ValueKind != JsonValueKind.Array)
            return null;

        CliVersion? latest = null;
        foreach (var element in versions.EnumerateArray())
        {
            var parsed = CliVersion.TryParse(element.GetString());
            if (parsed is null || parsed.IsPreRelease)
                continue;

            if (latest is null || parsed.IsNewerThan(latest))
                latest = parsed;
        }

        return latest;
    }

    private static Uri EnsureTrailingSlash(Uri uri)
        => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
}
