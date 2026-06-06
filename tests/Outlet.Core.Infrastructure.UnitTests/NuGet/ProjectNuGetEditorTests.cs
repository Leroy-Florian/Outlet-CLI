using System.Xml.Linq;
using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.NuGet;
using Outlet.Core.Infrastructure.UnitTests.Fakes;

namespace Outlet.Core.Infrastructure.UnitTests.NuGet;

public sealed class ProjectNuGetEditorTests
{
    private const string CsprojPath = "/repo/App.csproj";
    private const string CentralPath = "/repo/Directory.Packages.props";

    private const string EmptyCsproj = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Existing" Version="1.0.0" />
          </ItemGroup>
        </Project>
        """;

    private const string CentralProps = """
        <Project>
          <ItemGroup>
            <PackageVersion Include="Existing" Version="1.0.0" />
          </ItemGroup>
        </Project>
        """;

    private readonly FakeFileSystem _fileSystem = new();
    private readonly ProjectNuGetEditor _editor;

    public ProjectNuGetEditorTests()
    {
        _editor = new ProjectNuGetEditor(_fileSystem);
    }

    [Fact]
    public async Task Should_AddVersionedReference_When_NotCpmAndAbsent()
    {
        await Seed(CsprojPath, EmptyCsproj);

        var result = await _editor.AddPackageAsync(Request("MailKit", "4.16.0"));

        result.Outcome.Should().Be(NuGetEditOutcome.Added);
        Version(CsprojPath, "PackageReference", "MailKit").Should().Be("4.16.0");
    }

    [Fact]
    public async Task Should_BeIdempotent_When_SameVersionAlreadyPresent()
    {
        await Seed(CsprojPath, EmptyCsproj);

        var result = await _editor.AddPackageAsync(Request("Existing", "1.0.0"));

        result.Outcome.Should().Be(NuGetEditOutcome.AlreadySatisfied);
        Count(CsprojPath, "PackageReference", "Existing").Should().Be(1);
    }

    [Fact]
    public async Task Should_Warn_When_ExistingReferenceIsBelowFloor()
    {
        await Seed(CsprojPath, EmptyCsproj);

        var result = await _editor.AddPackageAsync(Request("Existing", "2.0.0"));

        result.Outcome.Should().Be(NuGetEditOutcome.Conflict);
        result.Warning.Should().Contain("below the floor");
        Version(CsprojPath, "PackageReference", "Existing").Should().Be("1.0.0", "conflicts are never overwritten");
    }

    [Fact]
    public async Task Should_TreatHigherExistingVersionAsSatisfied()
    {
        await Seed(CsprojPath, EmptyCsproj);

        var result = await _editor.AddPackageAsync(Request("Existing", "0.9.0"));

        result.Outcome.Should().Be(NuGetEditOutcome.AlreadySatisfied);
    }

    [Fact]
    public async Task Should_WriteCentralVersionAndVersionlessReference_When_CpmAndAbsent()
    {
        await Seed(CsprojPath, EmptyCsproj);
        await Seed(CentralPath, CentralProps);

        var result = await _editor.AddPackageAsync(Request("MailKit", "4.16.0", cpm: true));

        result.Outcome.Should().Be(NuGetEditOutcome.Added);
        Version(CentralPath, "PackageVersion", "MailKit").Should().Be("4.16.0");
        var reference = Find(CsprojPath, "PackageReference", "MailKit");
        reference!.Attribute("Version").Should().BeNull("under CPM the reference stays versionless");
    }

    [Fact]
    public async Task Should_NotDuplicate_When_CpmAddedTwice()
    {
        await Seed(CsprojPath, EmptyCsproj);
        await Seed(CentralPath, CentralProps);

        await _editor.AddPackageAsync(Request("MailKit", "4.16.0", cpm: true));
        var second = await _editor.AddPackageAsync(Request("MailKit", "4.16.0", cpm: true));

        second.Outcome.Should().Be(NuGetEditOutcome.AlreadySatisfied);
        Count(CentralPath, "PackageVersion", "MailKit").Should().Be(1);
        Count(CsprojPath, "PackageReference", "MailKit").Should().Be(1);
    }

    private NuGetEditRequest Request(string id, string version, bool cpm = false)
        => new(CsprojPath, PackageDependency.From(id, version), cpm, cpm ? CentralPath : null);

    private Task Seed(string path, string content) => _fileSystem.WriteAllTextAsync(path, content);

    private XElement? Find(string path, string element, string id)
        => XDocument.Parse(_fileSystem.Files[path])
            .Root!.Elements("ItemGroup").Elements(element)
            .FirstOrDefault(e => (string?)e.Attribute("Include") == id);

    private string? Version(string path, string element, string id)
        => (string?)Find(path, element, id)?.Attribute("Version");

    private int Count(string path, string element, string id)
        => XDocument.Parse(_fileSystem.Files[path])
            .Root!.Elements("ItemGroup").Elements(element)
            .Count(e => (string?)e.Attribute("Include") == id);
}
