using Outlet.Core.Infrastructure.Projects;

namespace Outlet.Core.Infrastructure.UnitTests.Projects;

public sealed class MsBuildOutputParserTests
{
    private const string Json = """
        {
          "Properties": {
            "TargetFramework": "net10.0",
            "TargetFrameworks": "",
            "RootNamespace": "Acme.App",
            "ManagePackageVersionsCentrally": "true"
          },
          "Items": {
            "PackageReference": [
              { "Identity": "xunit", "Version": "2.9.3" },
              { "Identity": "MailKit", "Version": "" }
            ]
          }
        }
        """;

    [Fact]
    public void Should_ParseEvaluatedProperties()
    {
        var evaluation = MsBuildOutputParser.Parse(Json);

        evaluation.Properties["TargetFramework"].Should().Be("net10.0");
        evaluation.Properties["RootNamespace"].Should().Be("Acme.App");
        evaluation.Properties["ManagePackageVersionsCentrally"].Should().Be("true");
    }

    [Fact]
    public void Should_ParsePackageReferencesWithMetadata()
    {
        var evaluation = MsBuildOutputParser.Parse(Json);

        evaluation.PackageReferences.Should().HaveCount(2);
        var xunit = evaluation.PackageReferences.Single(p => p.Identity == "xunit");
        xunit.Metadata["Version"].Should().Be("2.9.3");
    }

    [Fact]
    public void Should_ReturnEmptyPackageReferences_When_NoItemsSection()
    {
        var evaluation = MsBuildOutputParser.Parse("""{ "Properties": { "TargetFramework": "net10.0" } }""");

        evaluation.PackageReferences.Should().BeEmpty();
        evaluation.Properties["TargetFramework"].Should().Be("net10.0");
    }
}
