using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Infrastructure.Manifests;

/// <summary>
/// Parses, validates and writes item manifests (*.registry.json).
///
/// JSON lives ONLY here, in Infrastructure (the hexagon stays JSON-free).
/// Parsing never throws on malformed input: it returns a <see cref="Result{T}"/>
/// carrying a human-readable error, so callers can report cleanly.
/// </summary>
public static class RegistryItemManifestSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Deserializes and validates a manifest document.</summary>
    public static Result<RegistryItemManifest> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result<RegistryItemManifest>.Failure("Manifest content is empty.");

        ManifestJson? raw;
        try
        {
            raw = JsonSerializer.Deserialize<ManifestJson>(json, Options);
        }
        catch (JsonException ex)
        {
            return Result<RegistryItemManifest>.Failure($"Manifest is not valid JSON: {ex.Message}");
        }

        return raw is null
            ? Result<RegistryItemManifest>.Failure("Manifest deserialized to null.")
            : Validate(raw);
    }

    /// <summary>Serializes a manifest back to its canonical, indented JSON form.</summary>
    public static string Serialize(RegistryItemManifest manifest)
        => JsonSerializer.Serialize(manifest, Options);

    /// <summary>Serializes the aggregate registry index — <c>{ "items": [ … ] }</c> — served to clients.</summary>
    public static string SerializeIndex(IReadOnlyList<RegistryItemManifest> manifests)
        => JsonSerializer.Serialize(new RegistryIndexDocument(manifests), Options);

    /// <summary>
    /// Deserializes and validates a registry index document — <c>{ "items": [ &lt;manifest&gt;, … ] }</c> —
    /// the aggregate a remote registry serves so the engine can list/resolve in one round-trip.
    /// </summary>
    public static Result<IReadOnlyList<RegistryItemManifest>> ParseIndex(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result<IReadOnlyList<RegistryItemManifest>>.Failure("Registry index content is empty.");

        IndexJson? raw;
        try
        {
            raw = JsonSerializer.Deserialize<IndexJson>(json, Options);
        }
        catch (JsonException ex)
        {
            return Result<IReadOnlyList<RegistryItemManifest>>.Failure($"Registry index is not valid JSON: {ex.Message}");
        }

        if (raw?.Items is null)
            return Result<IReadOnlyList<RegistryItemManifest>>.Failure("Registry index is missing an 'items' array.");

        var manifests = new List<RegistryItemManifest>();
        foreach (var entry in raw.Items)
        {
            var validated = Validate(entry);
            if (validated.IsFailure)
                return Result<IReadOnlyList<RegistryItemManifest>>.Failure(validated.Error!);
            manifests.Add(validated.Value!);
        }

        return Result<IReadOnlyList<RegistryItemManifest>>.Success(manifests);
    }

    /// <summary>
    /// Maps a validated manifest onto the Domain aggregate. Re-checks the Domain
    /// invariants (kebab-case id, single-word concern, contract-has-no-NuGet, …)
    /// and surfaces any violation as a failed <see cref="Result{T}"/>.
    /// </summary>
    public static Result<RegistryItem> ToRegistryItem(RegistryItemManifest manifest)
    {
        try
        {
            var type = manifest.IsContract ? RegistryItemType.Contract : RegistryItemType.Adapter;

            return RegistryItem.Create(
                RegistryItemId.From(manifest.Name),
                ConcernName.From(manifest.Concern),
                type,
                [.. manifest.Files.Select(f => f.Path)],
                [.. manifest.RegistryDependencies.Select(RegistryItemId.From)],
                [.. manifest.NugetDependencies.Select(d => PackageDependency.From(d.Id, d.Version))]);
        }
        catch (ArgumentException ex)
        {
            return Result<RegistryItem>.Failure($"Manifest '{manifest.Name}' is invalid: {ex.Message}");
        }
    }

    private static Result<RegistryItemManifest> Validate(ManifestJson raw)
    {
        if (string.IsNullOrWhiteSpace(raw.Name))
            return Fail("'name' is required.");

        if (raw.Type is not (RegistryItemManifest.ContractType or RegistryItemManifest.AdapterType))
            return Fail(
                $"'type' must be '{RegistryItemManifest.ContractType}' or '{RegistryItemManifest.AdapterType}', " +
                $"but was '{raw.Type ?? "(missing)"}'.");

        if (string.IsNullOrWhiteSpace(raw.Concern))
            return Fail("'concern' is required.");

        if (raw.TargetFrameworks is not { Count: > 0 })
            return Fail("'targetFrameworks' must list at least one framework.");

        if (raw.Files is not { Count: > 0 })
            return Fail("'files' must list at least one file.");

        foreach (var file in raw.Files)
        {
            if (string.IsNullOrWhiteSpace(file.Path) || string.IsNullOrWhiteSpace(file.Target))
                return Fail("every file must declare a non-empty 'path' and 'target'.");
        }

        foreach (var dependency in raw.NugetDependencies ?? [])
        {
            if (string.IsNullOrWhiteSpace(dependency.Id) || string.IsNullOrWhiteSpace(dependency.Version))
                return Fail("every NuGet dependency must declare a non-empty 'id' and 'version'.");
        }

        var isContract = raw.Type == RegistryItemManifest.ContractType;
        if (isContract && raw.NugetDependencies is { Count: > 0 })
            return Fail(
                $"contract item '{raw.Name}' must declare zero NuGet dependencies, " +
                $"but declares {raw.NugetDependencies.Count}.");

        var manifest = new RegistryItemManifest(
            raw.Name!,
            raw.Type!,
            raw.Concern!,
            raw.Description,
            [.. raw.TargetFrameworks],
            [.. raw.RegistryDependencies ?? []],
            [.. (raw.NugetDependencies ?? []).Select(d => new ManifestNugetDependency(d.Id!, d.Version!))],
            [.. raw.Files.Select(f => new ManifestFile(f.Path!, f.Target!))]);

        return Result<RegistryItemManifest>.Success(manifest);
    }

    private static Result<RegistryItemManifest> Fail(string reason)
        => Result<RegistryItemManifest>.Failure($"Invalid manifest: {reason}");

    // Lenient deserialization shape: every member is nullable so a malformed
    // document is reported by Validate() rather than throwing on bind.
    private sealed class ManifestJson
    {
        public string? Name { get; init; }
        public string? Type { get; init; }
        public string? Concern { get; init; }
        public string? Description { get; init; }
        public List<string>? TargetFrameworks { get; init; }
        public List<string>? RegistryDependencies { get; init; }
        public List<NugetJson>? NugetDependencies { get; init; }
        public List<FileJson>? Files { get; init; }
    }

    private sealed class NugetJson
    {
        public string? Id { get; init; }
        public string? Version { get; init; }
    }

    private sealed class FileJson
    {
        public string? Path { get; init; }
        public string? Target { get; init; }
    }

    private sealed class IndexJson
    {
        public List<ManifestJson>? Items { get; init; }
    }

    private sealed record RegistryIndexDocument(IReadOnlyList<RegistryItemManifest> Items);
}
