using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.Manifests;

namespace Outlet.Core.Infrastructure.UnitTests.Manifests;

public sealed class RegistryItemManifestSerializerTests
{
    private const string ValidAdapterJson = """
        {
          "name": "email-smtp",
          "type": "outlet:adapter",
          "concern": "email",
          "description": "SMTP adapter for IEmailSender (MailKit).",
          "targetFrameworks": ["net8.0", "net9.0", "net10.0"],
          "registryDependencies": ["email-abstractions"],
          "nugetDependencies": [{ "id": "MailKit", "version": "4.8.0" }],
          "files": [
            { "path": "SmtpEmailSender.cs", "target": "adapter" },
            { "path": "SmtpEmailOptions.cs", "target": "adapter" }
          ]
        }
        """;

    [Fact]
    public void Should_ParseAllFields_When_ManifestIsValid()
    {
        var result = RegistryItemManifestSerializer.Parse(ValidAdapterJson);

        result.IsSuccess.Should().BeTrue(result.Error);
        var manifest = result.Value!;
        manifest.Name.Should().Be("email-smtp");
        manifest.Type.Should().Be("outlet:adapter");
        manifest.Concern.Should().Be("email");
        manifest.Description.Should().Be("SMTP adapter for IEmailSender (MailKit).");
        manifest.TargetFrameworks.Should().Equal("net8.0", "net9.0", "net10.0");
        manifest.RegistryDependencies.Should().ContainSingle().Which.Should().Be("email-abstractions");
        manifest.NugetDependencies.Should().ContainSingle();
        manifest.NugetDependencies[0].Id.Should().Be("MailKit");
        manifest.NugetDependencies[0].Version.Should().Be("4.8.0");
        manifest.Files.Should().HaveCount(2);
        manifest.IsContract.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_JsonIsMalformed()
    {
        var result = RegistryItemManifestSerializer.Parse("{ not json");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not valid JSON");
    }

    [Fact]
    public void Should_Fail_When_ContentIsEmpty()
    {
        var result = RegistryItemManifestSerializer.Parse("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Theory]
    [InlineData("outlet:provider")]
    [InlineData("")]
    public void Should_Fail_When_TypeIsNotInVocabulary(string type)
    {
        var json = $$"""
            {
              "name": "email-smtp",
              "type": "{{type}}",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "files": [{ "path": "X.cs", "target": "adapter" }]
            }
            """;

        var result = RegistryItemManifestSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("'type'");
    }

    [Fact]
    public void Should_Fail_When_NoFilesAreDeclared()
    {
        var json = """
            {
              "name": "email-abstractions",
              "type": "outlet:contract",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "files": []
            }
            """;

        var result = RegistryItemManifestSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("'files'");
    }

    [Fact]
    public void Should_Fail_When_NoTargetFrameworksAreDeclared()
    {
        var json = """
            {
              "name": "email-abstractions",
              "type": "outlet:contract",
              "concern": "email",
              "targetFrameworks": [],
              "files": [{ "path": "IEmailSender.cs", "target": "contract" }]
            }
            """;

        var result = RegistryItemManifestSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("'targetFrameworks'");
    }

    [Fact]
    public void Should_Fail_When_ContractDeclaresNugetDependencies()
    {
        var json = """
            {
              "name": "email-abstractions",
              "type": "outlet:contract",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "nugetDependencies": [{ "id": "MailKit", "version": "4.8.0" }],
              "files": [{ "path": "IEmailSender.cs", "target": "contract" }]
            }
            """;

        var result = RegistryItemManifestSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("zero NuGet dependencies");
    }

    [Fact]
    public void Should_Fail_When_NugetDependencyMissesVersion()
    {
        var json = """
            {
              "name": "email-smtp",
              "type": "outlet:adapter",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "nugetDependencies": [{ "id": "MailKit" }],
              "files": [{ "path": "SmtpEmailSender.cs", "target": "adapter" }]
            }
            """;

        var result = RegistryItemManifestSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("'id' and 'version'");
    }

    [Fact]
    public void Should_RoundTrip_When_SerializedThenParsed()
    {
        var original = RegistryItemManifestSerializer.Parse(ValidAdapterJson).Value!;

        var json = RegistryItemManifestSerializer.Serialize(original);
        var reparsed = RegistryItemManifestSerializer.Parse(json);

        reparsed.IsSuccess.Should().BeTrue(reparsed.Error);
        reparsed.Value.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Should_MapToDomainAggregate_When_ManifestIsValid()
    {
        var manifest = RegistryItemManifestSerializer.Parse(ValidAdapterJson).Value!;

        var result = RegistryItemManifestSerializer.ToRegistryItem(manifest);

        result.IsSuccess.Should().BeTrue(result.Error);
        var item = result.Value!;
        item.Id.Value.Should().Be("email-smtp");
        item.Concern.Value.Should().Be("email");
        item.Type.Should().Be(RegistryItemType.Adapter);
        item.Files.Should().Equal("SmtpEmailSender.cs", "SmtpEmailOptions.cs");
        item.RegistryDependencies.Should().ContainSingle().Which.Value.Should().Be("email-abstractions");
        item.NugetDependencies.Should().ContainSingle().Which.PackageId.Should().Be("MailKit");
    }

    [Fact]
    public void Should_ParseAndRoundTrip_When_FileCarriesAHash()
    {
        var hash = new string('a', 64);
        var json = $$"""
            {
              "name": "email-smtp",
              "type": "outlet:adapter",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "files": [{ "path": "SmtpEmailSender.cs", "target": "adapter", "hash": "{{hash}}" }]
            }
            """;

        var parsed = RegistryItemManifestSerializer.Parse(json);

        parsed.IsSuccess.Should().BeTrue(parsed.Error);
        parsed.Value!.Files[0].Hash.Should().Be(hash);

        var reparsed = RegistryItemManifestSerializer.Parse(RegistryItemManifestSerializer.Serialize(parsed.Value));
        reparsed.Value!.Files[0].Hash.Should().Be(hash);
    }

    [Fact]
    public void Should_LeaveHashNull_When_FileOmitsIt()
    {
        var parsed = RegistryItemManifestSerializer.Parse(ValidAdapterJson);

        parsed.Value!.Files.Should().OnlyContain(f => f.Hash == null);
    }

    [Fact]
    public void Should_Fail_When_FileHashIsNotSha256Hex()
    {
        var json = """
            {
              "name": "email-smtp",
              "type": "outlet:adapter",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "files": [{ "path": "SmtpEmailSender.cs", "target": "adapter", "hash": "not-a-hash" }]
            }
            """;

        var result = RegistryItemManifestSerializer.Parse(json);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("SHA-256");
    }

    [Fact]
    public void Should_FailMapping_When_NameIsNotKebabCase()
    {
        var manifest = new RegistryItemManifest(
            "Email_Smtp",
            RegistryItemManifest.AdapterType,
            "email",
            null,
            ["net10.0"],
            [],
            [],
            [new ManifestFile("X.cs", "adapter")]);

        var result = RegistryItemManifestSerializer.ToRegistryItem(manifest);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("invalid");
    }
}
