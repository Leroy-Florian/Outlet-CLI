using System.Net;
using Outlet.Core.Application.RegistryItems;
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
/// When the index carries a per-file hash (stamped by the catalogue builder), a
/// downloaded file is verified against it: a mismatch means the served bytes no
/// longer match the manifest they were listed under (corruption, a truncated
/// transfer, a tampered mirror) and is refused. This is integrity, not provenance —
/// it does not vouch for a registry you do not already trust.
/// </summary>
public sealed class HttpRegistrySource(HttpClient httpClient, Uri baseUri) : IRegistrySource
{
    private readonly Uri _baseUri = EnsureTrailingSlash(baseUri);

    // The index is small and re-read for every file fetch (hash lookup); cache the
    // parsed result per instance so a multi-file install hits the network once.
    private Task<IReadOnlyList<RegistryItemManifest>>? _manifests;

    public async Task<IReadOnlyList<RegistryItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<RegistryItem>();
        foreach (var manifest in await GetManifestsAsync(cancellationToken))
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
        var content = await GetStringOrNullAsync(uri, cancellationToken)
            ?? throw new InvalidOperationException(
                $"File '{filePath}' of item '{id.Value}' was not found at {uri}.");

        var expectedHash = await ExpectedHashAsync(id, filePath, cancellationToken);
        if (expectedHash is not null && !string.Equals(ContentHash.Of(content), expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"File '{filePath}' of item '{id.Value}' served from {_baseUri} does not match the hash declared " +
                "in the registry index — the file was altered, truncated, or tampered with in transit. Refusing to install it.");

        return content;
    }

    private async Task<string?> ExpectedHashAsync(RegistryItemId id, string filePath, CancellationToken cancellationToken)
    {
        var manifest = (await GetManifestsAsync(cancellationToken)).FirstOrDefault(m => m.Name == id.Value);
        return manifest?.Files.FirstOrDefault(f => f.Path == filePath)?.Hash;
    }

    private Task<IReadOnlyList<RegistryItemManifest>> GetManifestsAsync(CancellationToken cancellationToken)
        => _manifests ??= LoadManifestsAsync(cancellationToken);

    private async Task<IReadOnlyList<RegistryItemManifest>> LoadManifestsAsync(CancellationToken cancellationToken)
    {
        var json = await GetStringOrNullAsync(new Uri(_baseUri, "registry.json"), cancellationToken);
        if (json is null)
            return [];

        var parsed = RegistryItemManifestSerializer.ParseIndex(json);
        if (parsed.IsFailure)
            throw new InvalidOperationException($"Registry index at {_baseUri} is invalid: {parsed.Error}");

        return parsed.Value!;
    }

    private async Task<string?> GetStringOrNullAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Uri EnsureTrailingSlash(Uri uri)
        => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
}
