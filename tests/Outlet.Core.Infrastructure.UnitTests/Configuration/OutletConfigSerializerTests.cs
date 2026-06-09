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
    public void Should_ParseAndRoundTripAuth_When_RegistryDeclaresIt()
    {
        var json = """
            {
              "registries": [ { "name": "acme", "url": "https://acme.test/", "auth": { "scheme": "Bearer", "tokenEnv": "ACME_TOKEN" } } ],
              "targets": { "contract": { "project": "A.csproj", "namespace": "A" }, "adapter": { "project": "A.csproj", "namespace": "A" } }
            }
            """;

        var parsed = OutletConfigSerializer.Parse(json);

        parsed.IsSuccess.Should().BeTrue(parsed.Error);
        var auth = parsed.Value!.Registries.Single().Auth;
        auth.Should().NotBeNull();
        auth!.Scheme.Should().Be("Bearer");
        auth.TokenEnv.Should().Be("ACME_TOKEN");

        var reparsed = OutletConfigSerializer.Parse(OutletConfigSerializer.Serialize(parsed.Value!));
        reparsed.Value!.Registries.Single().Auth.Should().Be(auth);
    }

    [Fact]
    public void Should_DefaultSchemeToBearer_When_OnlyTokenEnvGiven()
    {
        var json = """
            {
              "registries": [ { "name": "acme", "url": "https://acme.test/", "auth": { "tokenEnv": "ACME_TOKEN" } } ],
              "targets": { "contract": { "project": "A.csproj", "namespace": "A" }, "adapter": { "project": "A.csproj", "namespace": "A" } }
            }
            """;

        var parsed = OutletConfigSerializer.Parse(json);

        parsed.Value!.Registries.Single().Auth!.Scheme.Should().Be("Bearer");
    }

    [Fact]
    public void Should_Fail_When_AuthHasNoTokenEnv()
    {
        var json = """
            {
              "registries": [ { "name": "acme", "url": "https://acme.test/", "auth": { "scheme": "Bearer" } } ],
              "targets": { "contract": { "project": "A.csproj", "namespace": "A" }, "adapter": { "project": "A.csproj", "namespace": "A" } }
            }
            """;

        var result = OutletConfigSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("tokenEnv");
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
}
