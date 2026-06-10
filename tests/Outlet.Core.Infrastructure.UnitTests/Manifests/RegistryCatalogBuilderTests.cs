using System.Runtime.CompilerServices;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Infrastructure.Manifests;

namespace Outlet.Core.Infrastructure.UnitTests.Manifests;

public sealed class RegistryCatalogBuilderTests
{
    [Fact]
    public void Should_BuildValidCatalog_FromTheRealRegistry()
    {
        var registryRoot = Path.Combine(RepoRoot(), "registry");

        var result = RegistryCatalogBuilder.Build(registryRoot);

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Items.Select(i => i.Manifest.Name)
            .Should().Contain(["email-abstractions", "email-smtp", "email-sendgrid"]);

        var index = RegistryItemManifestSerializer.ParseIndex(result.Value.IndexJson);
        index.IsSuccess.Should().BeTrue(index.Error);
        index.Value!.Count.Should().Be(result.Value.Items.Count);
    }

    [Fact]
    public void Should_Fail_When_AManifestDeclaresAMissingFile()
    {
        var root = CreateTempRegistry(
            ("email/ghost/ghost.registry.json", """
                {
                  "name": "ghost-item",
                  "type": "outlet:adapter",
                  "concern": "email",
                  "targetFrameworks": ["net10.0"],
                  "files": [{ "path": "Ghost.cs", "target": "adapter" }]
                }
                """));

        try
        {
            var result = RegistryCatalogBuilder.Build(root);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("missing on disk");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Should_Fail_When_TwoItemsShareAName()
    {
        var manifest = """
            {
              "name": "dup",
              "type": "outlet:contract",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "files": [{ "path": "F.cs", "target": "contract" }]
            }
            """;
        var root = CreateTempRegistry(
            ("a/dup.registry.json", manifest),
            ("b/dup.registry.json", manifest));
        WriteFile(root, "a/F.cs", "// f");
        WriteFile(root, "b/F.cs", "// f");

        try
        {
            var result = RegistryCatalogBuilder.Build(root);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Duplicate item name");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Should_StampEachFileHash_IntoThePublishedIndex()
    {
        const string fileContent = "public sealed class Ghost { }\n";
        var root = CreateTempRegistry(
            ("email/ghost/ghost.registry.json", """
                {
                  "name": "ghost-item",
                  "type": "outlet:adapter",
                  "concern": "email",
                  "targetFrameworks": ["net10.0"],
                  "files": [{ "path": "Ghost.cs", "target": "adapter" }]
                }
                """));
        WriteFile(root, "email/ghost/Ghost.cs", fileContent);

        try
        {
            var result = RegistryCatalogBuilder.Build(root);

            result.IsSuccess.Should().BeTrue(result.Error);
            var index = RegistryItemManifestSerializer.ParseIndex(result.Value!.IndexJson).Value!;
            var file = index.Single(m => m.Name == "ghost-item").Files.Single();
            file.Hash.Should().Be(ContentHash.Of(fileContent));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRegistry(params (string RelativePath, string Content)[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), "outlet-catalog-" + Guid.NewGuid().ToString("N"));
        foreach (var (relativePath, content) in files)
            WriteFile(root, relativePath, content);
        return root;
    }

    private static void WriteFile(string root, string relativePath, string content)
    {
        var fullPath = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private static string RepoRoot([CallerFilePath] string callerPath = "")
    {
        var directory = Path.GetDirectoryName(callerPath)!;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(directory, "Outlet.slnx")))
                return directory;
            directory = Path.GetDirectoryName(directory)!;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
