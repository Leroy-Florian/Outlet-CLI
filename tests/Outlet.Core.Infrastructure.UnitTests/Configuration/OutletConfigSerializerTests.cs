using Outlet.Core.Application.Configuration;
using Outlet.Core.Infrastructure.Configuration;

namespace Outlet.Core.Infrastructure.UnitTests.Configuration;

public sealed class OutletConfigSerializerTests
{
    private const string ValidJson = """
        {
          "registries": [ { "name": "outlet", "url": "https://registry.outlet.dev/" } ],
          "targets": {
            "contract": { "project": "src/App/App.csproj", "namespace": "App" },
            "adapter":  { "project": "src/App/App.csproj", "namespace": "App.Infrastructure" }
          },
          "installed": [
            {
              "name": "email-smtp",
              "version": "1.0.0",
              "files": [{ "path": "Email/SmtpEmailSender.cs", "hash": "ABC123" }],
              "packages": [ { "id": "MailKit", "version": "4.16.0" } ]
            }
          ]
        }
        """;

    [Fact]
    public void Should_ParseAllSections_When_Valid()
    {
        var result = OutletConfigSerializer.Parse(ValidJson);

        result.IsSuccess.Should().BeTrue(result.Error);
        var config = result.Value!;
        config.Registries.Should().ContainSingle().Which.Url.Should().Be("https://registry.outlet.dev/");
        config.Targets.Adapter.Namespace.Should().Be("App.Infrastructure");
        config.Installed.Should().ContainSingle();
        config.Installed[0].Packages.Should().ContainSingle().Which.Id.Should().Be("MailKit");
    }

    [Fact]
    public void Should_RoundTrip_When_SerializedThenParsed()
    {
        var original = OutletConfigSerializer.Parse(ValidJson).Value!;

        var reparsed = OutletConfigSerializer.Parse(OutletConfigSerializer.Serialize(original));

        reparsed.IsSuccess.Should().BeTrue(reparsed.Error);
        reparsed.Value.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Should_Fail_When_TargetsAreMissing()
    {
        var json = """{ "registries": [], "installed": [] }""";

        var result = OutletConfigSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("'targets'");
    }

    [Fact]
    public void Should_Fail_When_RegistryUrlIsNotAbsolute()
    {
        var json = """
            {
              "registries": [ { "name": "local", "url": "registry.json" } ],
              "targets": { "contract": { "project": "A.csproj", "namespace": "A" }, "adapter": { "project": "A.csproj", "namespace": "A" } }
            }
            """;

        var result = OutletConfigSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("non-absolute url");
    }

    [Fact]
    public void Should_ProduceValidDefault_When_CreateDefaultIsUsed()
    {
        var config = OutletConfig.CreateDefault("src/App/App.csproj", "App");

        var reparsed = OutletConfigSerializer.Parse(OutletConfigSerializer.Serialize(config));

        reparsed.IsSuccess.Should().BeTrue(reparsed.Error);
        reparsed.Value!.Targets.Contract.Should().Be(reparsed.Value.Targets.Adapter);
        reparsed.Value.Installed.Should().BeEmpty();
    }

    [Fact]
    public void Should_MarkTheDefaultOfficialRegistryTrusted()
    {
        var config = OutletConfig.CreateDefault("src/App/App.csproj", "App");

        config.Registries.Should().ContainSingle().Which.Trusted.Should().BeTrue();
    }

    [Fact]
    public void Should_DefaultRegistryToUntrusted_When_TrustedIsAbsent()
    {
        var json = """
            {
              "registries": [ { "name": "corp", "url": "https://corp.example/" } ],
              "targets": { "contract": { "project": "A.csproj", "namespace": "A" }, "adapter": { "project": "A.csproj", "namespace": "A" } }
            }
            """;

        var result = OutletConfigSerializer.Parse(json);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Registries.Should().ContainSingle().Which.Trusted.Should().BeFalse();
    }

    [Fact]
    public void Should_RoundTripTrust_When_RegistryIsTrusted()
    {
        var json = """
            {
              "registries": [ { "name": "corp", "url": "https://corp.example/", "trusted": true } ],
              "targets": { "contract": { "project": "A.csproj", "namespace": "A" }, "adapter": { "project": "A.csproj", "namespace": "A" } }
            }
            """;
        var parsed = OutletConfigSerializer.Parse(json).Value!;

        var reparsed = OutletConfigSerializer.Parse(OutletConfigSerializer.Serialize(parsed));

        reparsed.Value!.Registries.Single().Trusted.Should().BeTrue();
    }
}
