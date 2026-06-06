using Outlet.Core.Infrastructure.Manifests;

// Generates the published registry (dist/registry/): validates every *.registry.json,
// checks declared files exist, then writes the aggregate index + per-item file payloads.
// Usage: outlet-registry-builder [registryRoot] [outputDir]

var registryRoot = args.Length > 0 ? args[0] : "registry";
var outputDir = args.Length > 1 ? args[1] : Path.Combine("dist", "registry");

var result = RegistryCatalogBuilder.Build(registryRoot);
if (result.IsFailure)
{
    Console.Error.WriteLine($"error: {result.Error}");
    return 1;
}

var catalog = result.Value!;

Directory.CreateDirectory(outputDir);
File.WriteAllText(Path.Combine(outputDir, "registry.json"), catalog.IndexJson);

foreach (var item in catalog.Items)
{
    var itemOutput = Path.Combine(outputDir, item.Manifest.Name);
    foreach (var file in item.Files)
    {
        var destination = Path.Combine(itemOutput, file);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(Path.Combine(item.ItemDirectory, file), destination, overwrite: true);
    }
}

Console.WriteLine($"Generated {catalog.Items.Count} item(s) into '{outputDir}':");
foreach (var item in catalog.Items)
    Console.WriteLine($"  - {item.Manifest.Name} ({item.Files.Count} file(s))");

return 0;
