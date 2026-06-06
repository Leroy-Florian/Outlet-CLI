using System.Text.Json;

namespace Outlet.Core.Infrastructure.Projects;

/// <summary>
/// Parses the JSON emitted by <c>dotnet msbuild -getProperty:… -getItem:…</c>
/// (the EVALUATED values) into an <see cref="MsBuildEvaluation"/>. Pure, so the
/// shape of MSBuild's output is pinned by hermetic tests.
/// </summary>
public static class MsBuildOutputParser
{
    public static MsBuildEvaluation Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("Properties", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in props.EnumerateObject())
                properties[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        var packageReferences = new List<MsBuildItem>();
        if (root.TryGetProperty("Items", out var items)
            && items.ValueKind == JsonValueKind.Object
            && items.TryGetProperty("PackageReference", out var entries)
            && entries.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entries.EnumerateArray())
            {
                var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var identity = string.Empty;

                foreach (var field in entry.EnumerateObject())
                {
                    var value = field.Value.ValueKind == JsonValueKind.String
                        ? field.Value.GetString() ?? string.Empty
                        : field.Value.ToString();

                    if (field.NameEquals("Identity"))
                        identity = value;
                    else
                        metadata[field.Name] = value;
                }

                if (identity.Length > 0)
                    packageReferences.Add(new MsBuildItem(identity, metadata));
            }
        }

        return new MsBuildEvaluation(properties, packageReferences);
    }
}
