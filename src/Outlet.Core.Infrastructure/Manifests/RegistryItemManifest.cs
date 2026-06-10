namespace Outlet.Core.Infrastructure.Manifests;

/// <summary>
/// Faithful in-memory representation of an item manifest (*.registry.json).
/// This is the wire/disk DTO — immutable, validated at parse time by
/// <see cref="RegistryItemManifestSerializer"/>, and mapped to the Domain
/// aggregate <c>RegistryItem</c> only once it is known to be well-formed.
/// </summary>
public sealed record RegistryItemManifest(
    string Name,
    string Type,
    string Concern,
    string? Description,
    IReadOnlyList<string> TargetFrameworks,
    IReadOnlyList<string> RegistryDependencies,
    IReadOnlyList<ManifestNugetDependency> NugetDependencies,
    IReadOnlyList<ManifestFile> Files)
{
    /// <summary>Manifest vocabulary for a contract item (generic port + DTOs).</summary>
    public const string ContractType = "outlet:contract";

    /// <summary>Manifest vocabulary for an adapter item (provider implementation).</summary>
    public const string AdapterType = "outlet:adapter";

    public bool IsContract => Type == ContractType;
}

/// <summary>A direct NuGet dependency as declared in the manifest (version is a floor).</summary>
public sealed record ManifestNugetDependency(string Id, string Version);

/// <summary>
/// A file shipped by the item: source <paramref name="Path"/> + routing <paramref name="Target"/>.
/// <paramref name="Hash"/> is the SHA-256 (hex) of the file's content, stamped into the published
/// index by the catalogue builder so a client can detect a served file that no longer matches the
/// manifest it was listed under (corruption, a truncated transfer, a tampered mirror). It is left
/// null on hand-written source manifests — authors never compute it — and is not a substitute for
/// trusting the registry's origin (a hostile registry signs its own lies).
/// </summary>
public sealed record ManifestFile(string Path, string Target, string? Hash = null);
