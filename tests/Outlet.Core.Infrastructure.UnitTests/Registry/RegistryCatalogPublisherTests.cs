using Outlet.Core.Infrastructure.Registry;

namespace Outlet.Core.Infrastructure.UnitTests.Registry;

public sealed class RegistryCatalogPublisherTests
{
    private const string Manifest = """
        {
          "name": "email-smtp",
          "type": "outlet:adapter",
          "concern": "email",
          "targetFrameworks": ["net10.0"],
          "files": [{ "path": "SmtpEmailSender.cs", "target": "adapter" }]
        }
        """;

    [Fact]
    public async Task Should_WriteIndexAndItemFiles_When_Publishing()
    {
        var (root, output) = CreateRegistry();
        try
        {
            var result = await new RegistryCatalogPublisher().PublishAsync(root, output, dryRun: false);

            result.IsSuccess.Should().BeTrue(result.Error);
            result.Value!.Items.Should().ContainSingle().Which.Name.Should().Be("email-smtp");
            File.Exists(Path.Combine(output, "registry.json")).Should().BeTrue();
            File.Exists(Path.Combine(output, "email-smtp", "SmtpEmailSender.cs")).Should().BeTrue();
        }
        finally
        {
            Cleanup(root, output);
        }
    }

    [Fact]
    public async Task Should_WriteNothing_When_DryRun()
    {
        var (root, output) = CreateRegistry();
        try
        {
            var result = await new RegistryCatalogPublisher().PublishAsync(root, output, dryRun: true);

            result.IsSuccess.Should().BeTrue(result.Error);
            result.Value!.DryRun.Should().BeTrue();
            result.Value.Items.Should().ContainSingle();
            Directory.Exists(output).Should().BeFalse("a dry run writes nothing to disk");
        }
        finally
        {
            Cleanup(root, output);
        }
    }

    [Fact]
    public async Task Should_Fail_When_RegistryIsMalformed()
    {
        var (root, output) = CreateRegistry(includeFile: false);
        try
        {
            var result = await new RegistryCatalogPublisher().PublishAsync(root, output, dryRun: false);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("missing on disk");
            Directory.Exists(output).Should().BeFalse("a failed publish writes nothing");
        }
        finally
        {
            Cleanup(root, output);
        }
    }

    private static (string Root, string Output) CreateRegistry(bool includeFile = true)
    {
        var root = Path.Combine(Path.GetTempPath(), "outlet-publish-" + Guid.NewGuid().ToString("N"));
        var itemDir = Path.Combine(root, "email", "smtp");
        Directory.CreateDirectory(itemDir);
        File.WriteAllText(Path.Combine(itemDir, "smtp.registry.json"), Manifest);
        if (includeFile)
            File.WriteAllText(Path.Combine(itemDir, "SmtpEmailSender.cs"), "public sealed class SmtpEmailSender;");

        return (root, root + "-out");
    }

    private static void Cleanup(string root, string output)
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
    }
}
