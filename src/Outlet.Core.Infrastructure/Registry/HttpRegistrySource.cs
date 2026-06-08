using System.Net;
using System.Net.Http.Headers;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.Manifests;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// SECONDARY ADAPTER — a registry source served over HTTP.
///
/// Layout: the index lives at <c>{baseUri}/registry.json</c> (an
/// <c>{ "items": [ … ] }</c> document) and each file at
/// <c>{baseUri}/{itemName}/{filePath}</c>. JSON parsing stays in the manifest
/// serializer; this adapter only does transport.
///
/// <paramref name="authorization"/> is attached per-request (never mutating the
/// shared <see cref="HttpClient"/>) so several sources can use distinct credentials.
/// Null = anonymous (public registry).
/// </summary>
public sealed class HttpRegistrySource(HttpClient httpClient, Uri baseUri, AuthenticationHeaderValue? authorization = null) : IRegistrySource
{
    private readonly Uri _baseUri = EnsureTrailingSlash(baseUri);

    public async Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetStringOrNullAsync(new Uri(_baseUri, "registry.json"), cancellationToken);
        if (json is null)
            return [];

        var parsed = RegistryItemManifestSerializer.ParseIndex(json);
        if (parsed.IsFailure)
            throw new InvalidOperationException($"Registry index at {_baseUri} is invalid: {parsed.Error}");

        var items = new List<RegistryItem>();
        foreach (var manifest in parsed.Value!)
        {
            var domain = RegistryItemManifestSerializer.ToRegistryItem(manifest);
            if (domain.IsSuccess)
                items.Add(domain.Value!);
        }

        return items;
    }

    public async Task<RegistryItem?> GetItemAsync(RegistryItemId id, CancellationToken cancellationToken = default)
    {
        var items = await GetItemsAsync(cancellationToken);
        return items.FirstOrDefault(i => i.Id.Value == id.Value);
    }

    public async Task<string> GetFileContentAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(_baseUri, $"{id.Value}/{filePath}");
        var content = await GetStringOrNullAsync(uri, cancellationToken);
        return content ?? throw new InvalidOperationException(
            $"File '{filePath}' of item '{id.Value}' was not found at {uri}.");
    }

    private async Task<string?> GetStringOrNullAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri) { Headers = { Authorization = authorization } };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Uri EnsureTrailingSlash(Uri uri)
        => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
}
