using Outlet.Core.Application.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Infrastructure.Manifests;

/// <summary>One catalogued item: its manifest, on-disk directory and declared file paths.</summary>
public sealed record RegistryCatalogItem(RegistryItemManifest Manifest, string ItemDirectory, IReadOnlyList<string> Files);

/// <summary>The built catalogue: the aggregate index JSON plus the items to publish.</summary>
public sealed record RegistryCatalog(string IndexJson, IReadOnlyList<RegistryCatalogItem> Items);

/// <summary>
/// Builds the published catalogue from the <c>registry/</c> tree: every
/// <c>*.registry.json</c> is parsed + validated, every declared file is checked to
/// exist on disk (the manifest must never lie), names must be unique, then the
/// aggregate index is produced. This is the executable gate behind "the manifest
/// never lies" — the generated artifact (HIJ-498), never hand-edited.
/// </summary>
public static class RegistryCatalogBuilder
{
    private const string ManifestSuffix = ".registry.json";

    public static Result<RegistryCatalog> Build(string registryRoot)
    {
        if (!Directory.Exists(registryRoot))
            return Result<RegistryCatalog>.Failure($"Registry root '{registryRoot}' does not exist.");

        List<string> manifestFiles =
        [
            .. Directory.EnumerateFiles(registryRoot, "*" + ManifestSuffix, SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal),
        ];

        var manifests = new List<RegistryItemManifest>();
        var items = new List<RegistryCatalogItem>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var manifestFile in manifestFiles)
        {
            var parsed = RegistryItemManifestSerializer.Parse(File.ReadAllText(manifestFile));
            if (parsed.IsFailure)
                return Result<RegistryCatalog>.Failure($"{manifestFile}: {parsed.Error}");

            var manifest = parsed.Value!;
            if (!seenNames.Add(manifest.Name))
                return Result<RegistryCatalog>.Failure($"Duplicate item name '{manifest.Name}'.");

            var itemDirectory = Path.GetDirectoryName(manifestFile)!;
            var hashedFiles = new List<ManifestFile>();
            foreach (var file in manifest.Files)
            {
                var sourcePath = Path.Combine(itemDirectory, file.Path);
                if (!File.Exists(sourcePath))
                    return Result<RegistryCatalog>.Failure(
                        $"Item '{manifest.Name}' declares file '{file.Path}' that is missing on disk.");

                // Stamp the served content's hash into the PUBLISHED manifest (authors never
                // hand-write it), so a client can detect a file that no longer matches the index.
                hashedFiles.Add(file with { Hash = ContentHash.Of(File.ReadAllText(sourcePath)) });
            }

            var publishedManifest = manifest with { Files = hashedFiles };
            manifests.Add(publishedManifest);
            items.Add(new RegistryCatalogItem(publishedManifest, itemDirectory, [.. manifest.Files.Select(f => f.Path)]));
        }

        return Result<RegistryCatalog>.Success(
            new RegistryCatalog(RegistryItemManifestSerializer.SerializeIndex(manifests), items));
    }
}
