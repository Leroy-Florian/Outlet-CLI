using System.Xml.Linq;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.NuGet;

/// <summary>
/// SECONDARY ADAPTER — adds direct PackageReference entries (floor versions),
/// CPM-aware: under central management the version goes to Directory.Packages.props
/// (<c>PackageVersion</c>) and the <c>PackageReference</c> stays versionless; otherwise
/// the <c>Version</c> goes on the reference. An existing direct reference at or above
/// the floor is left as-is; below the floor it is reported, never overwritten.
///
/// Editing the csproj/props XML is the legitimate WRITE path (the read/detection side
/// uses evaluated MSBuild values — see <c>MsBuildProjectInspector</c>).
/// </summary>
public sealed class ProjectNuGetEditor(IFileSystem fileSystem) : INuGetEditor
{
    public async Task<NuGetEditResult> AddPackageAsync(NuGetEditRequest request, CancellationToken cancellationToken = default)
    {
        var id = request.Dependency.PackageId;
        var version = request.Dependency.MinimumVersion;

        if (request.UsesCentralPackageManagement && !string.IsNullOrWhiteSpace(request.CentralPackagesFilePath))
        {
            var central = await EnsureCentralVersionAsync(request.CentralPackagesFilePath!, id, version, cancellationToken);
            await EnsureVersionlessReferenceAsync(request.ProjectFilePath, id, cancellationToken);
            return central;
        }

        return await EnsureVersionedReferenceAsync(request.ProjectFilePath, id, version, cancellationToken);
    }

    private async Task<NuGetEditResult> EnsureCentralVersionAsync(string centralFilePath, string id, string version, CancellationToken cancellationToken)
    {
        var document = await LoadAsync(centralFilePath, cancellationToken);
        var existing = FindItem(document, "PackageVersion", id);

        if (existing is not null)
            return Reconcile(id, version, (string?)existing.Attribute("Version"));

        GetOrCreateItemGroup(document, "PackageVersion")
            .Add(new XElement("PackageVersion", new XAttribute("Include", id), new XAttribute("Version", version)));

        await SaveAsync(centralFilePath, document, cancellationToken);
        return new NuGetEditResult(NuGetEditOutcome.Added);
    }

    private async Task EnsureVersionlessReferenceAsync(string projectFilePath, string id, CancellationToken cancellationToken)
    {
        var document = await LoadAsync(projectFilePath, cancellationToken);
        if (FindItem(document, "PackageReference", id) is not null)
            return;

        GetOrCreateItemGroup(document, "PackageReference")
            .Add(new XElement("PackageReference", new XAttribute("Include", id)));

        await SaveAsync(projectFilePath, document, cancellationToken);
    }

    private async Task<NuGetEditResult> EnsureVersionedReferenceAsync(string projectFilePath, string id, string version, CancellationToken cancellationToken)
    {
        var document = await LoadAsync(projectFilePath, cancellationToken);
        var existing = FindItem(document, "PackageReference", id);

        if (existing is not null)
        {
            var existingVersion = (string?)existing.Attribute("Version");
            // A versionless reference with no CPM is unusual project state; treat it as satisfied.
            return existingVersion is null
                ? new NuGetEditResult(NuGetEditOutcome.AlreadySatisfied)
                : Reconcile(id, version, existingVersion);
        }

        GetOrCreateItemGroup(document, "PackageReference")
            .Add(new XElement("PackageReference", new XAttribute("Include", id), new XAttribute("Version", version)));

        await SaveAsync(projectFilePath, document, cancellationToken);
        return new NuGetEditResult(NuGetEditOutcome.Added);
    }

    private static NuGetEditResult Reconcile(string id, string floor, string? existingVersion)
    {
        if (string.IsNullOrWhiteSpace(existingVersion))
            return new NuGetEditResult(NuGetEditOutcome.AlreadySatisfied);

        return CompareVersions(existingVersion, floor) >= 0
            ? new NuGetEditResult(NuGetEditOutcome.AlreadySatisfied)
            : new NuGetEditResult(
                NuGetEditOutcome.Conflict,
                $"Project already references '{id}' at {existingVersion}, below the floor {floor} required by the item; left unchanged.");
    }

    private static XElement? FindItem(XDocument document, string elementName, string id)
        => document.Root?
            .Elements("ItemGroup")
            .Elements(elementName)
            .FirstOrDefault(e => string.Equals((string?)e.Attribute("Include"), id, StringComparison.OrdinalIgnoreCase));

    private static XElement GetOrCreateItemGroup(XDocument document, string elementName)
    {
        var project = document.Root!;
        var group = project.Elements("ItemGroup").FirstOrDefault(g => g.Elements(elementName).Any());
        if (group is not null)
            return group;

        var created = new XElement("ItemGroup");
        project.Add(created);
        return created;
    }

    // Compares the numeric release parts (prerelease tags ignored) — enough to tell
    // "satisfies the floor" from "below the floor"; falls back to ordinal on parse failure.
    private static int CompareVersions(string left, string right)
    {
        var leftParts = ReleaseParts(left);
        var rightParts = ReleaseParts(right);

        for (var i = 0; i < Math.Max(leftParts.Length, rightParts.Length); i++)
        {
            var l = i < leftParts.Length ? leftParts[i] : 0;
            var r = i < rightParts.Length ? rightParts[i] : 0;
            if (l != r)
                return l.CompareTo(r);
        }

        return 0;
    }

    private static int[] ReleaseParts(string version)
    {
        var release = version.Split('-', '+')[0];
        var segments = release.Split('.');
        var parts = new int[segments.Length];
        for (var i = 0; i < segments.Length; i++)
            parts[i] = int.TryParse(segments[i], out var n) ? n : 0;
        return parts;
    }

    private async Task<XDocument> LoadAsync(string path, CancellationToken cancellationToken)
        => XDocument.Parse(await fileSystem.ReadAllTextAsync(path, cancellationToken));

    private Task SaveAsync(string path, XDocument document, CancellationToken cancellationToken)
        => fileSystem.WriteAllTextAsync(path, document.ToString(), cancellationToken);
}
