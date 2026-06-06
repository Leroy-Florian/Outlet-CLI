using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class UpdateItemUseCaseTests
{
    private const string ProjectDirectory = "/repo";
    private const string FilePath = "/repo/SmtpEmailSender.cs";

    private readonly FakeRegistryClient _registry = new();
    private readonly FakeProjectInspector _inspector = new();
    private readonly FakeFileSystem _fileSystem = new();
    private readonly FakeNuGetEditor _nuGet = new();
    private readonly FakeOutletConfigStore _config = new();

    private UpdateItemUseCase BuildUseCase()
        => new(_registry, _inspector, new FakeNamespaceRewriter(), _fileSystem, _nuGet, _config);

    private void Seed(string registryContent, string localContent, string baseContent)
    {
        var item = RegistryItem.Create(
            RegistryItemId.From("email-smtp"), ConcernName.From("email"), RegistryItemType.Adapter,
            ["SmtpEmailSender.cs"], [], [], ["net10.0"]).Value!;
        _registry.Seed(item);
        _registry.SeedFileContent(RegistryItemId.From("email-smtp"), "SmtpEmailSender.cs", registryContent);

        _config.Seed(OutletConfig.CreateDefault("App.csproj", "MyApp") with
        {
            Installed = [new InstalledItem("email-smtp", "0.0.0", [new InstalledFile("SmtpEmailSender.cs", ContentHash.Of(baseContent))], [], [])],
        });
        _fileSystem.Seed(FilePath, localContent);
    }

    [Fact]
    public async Task Should_ApplyUpdate_When_LocalIsUnedited()
    {
        Seed(registryContent: "v2", localContent: "v1", baseContent: "v1");

        var result = await BuildUseCase().HandleAsync(new UpdateItemCommand(ProjectDirectory, "email-smtp"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Updated.Should().Contain("SmtpEmailSender.cs");
        _fileSystem.Files[FilePath].Should().Be("v2");
        _config.Saved!.Installed.Single().Files.Single().Hash.Should().Be(ContentHash.Of("v2"));
    }

    [Fact]
    public async Task Should_KeepLocalEdits_When_RegistryUnchanged()
    {
        Seed(registryContent: "v1", localContent: "my edits", baseContent: "v1");

        var result = await BuildUseCase().HandleAsync(new UpdateItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.Updated.Should().BeEmpty();
        _fileSystem.Files[FilePath].Should().Be("my edits");
    }

    [Fact]
    public async Task Should_WriteOutletNew_And_KeepLocal_When_Conflict()
    {
        Seed(registryContent: "v2", localContent: "my edits", baseContent: "v1");

        var result = await BuildUseCase().HandleAsync(new UpdateItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.Conflicts.Should().Contain("SmtpEmailSender.cs");
        _fileSystem.Files[FilePath].Should().Be("my edits", "a conflict must never clobber local edits");
        _fileSystem.Files[FilePath + ".outlet-new"].Should().Be("v2");
    }

    [Fact]
    public async Task Should_Fail_When_ItemNotInstalled()
    {
        _config.Seed(OutletConfig.CreateDefault("App.csproj", "MyApp"));

        var result = await BuildUseCase().HandleAsync(new UpdateItemCommand(ProjectDirectory, "email-smtp"));

        result.IsFailure.Should().BeTrue();
    }
}
