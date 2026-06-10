namespace Outlet.Core.Infrastructure.Manifests;

/// <summary>
/// Writes a built <see cref="RegistryCatalog"/> to disk in the layout a remote serves:
/// the aggregate index at <c>{output}/registry.json</c> and each item's files under
/// <c>{output}/{itemName}/…</c>. Pure file IO — the validation that makes the catalogue
/// trustworthy already happened in <see cref="RegistryCatalogBuilder"/>.
/// </summary>
public static class RegistryCatalogWriter
{
    public static void Write(RegistryCatalog catalog, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "registry.json"), catalog.IndexJson);

        foreach (var item in catalog.Items)
        {
            var itemOutput = Path.Combine(outputDirectory, item.Manifest.Name);
            foreach (var file in item.Files)
            {
                var destination = Path.Combine(itemOutput, file);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Path.Combine(item.ItemDirectory, file), destination, overwrite: true);
            }
        }
    }
}
