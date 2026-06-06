using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class DiffItemUseCaseTests
{
    private const string ProjectDirectory = "/repo";
    private const string FilePath = "/repo/SmtpEmailSender.cs";

    private readonly FakeRegistryClient _registry = new();
    private readonly FakeFileSystem _fileSystem = new();
    private readonly FakeOutletConfigStore _config = new();

    private DiffItemUseCase BuildUseCase() => new(_registry, new FakeNamespaceRewriter(), _fileSystem, _config);

    // Plain content (no registry-namespace token) → the fake rewriter is identity, so new == registryContent.
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
    public async Task Should_ReportUpdateAvailable_When_LocalIsOriginalAndRegistryChanged()
    {
        Seed(registryContent: "v2", localContent: "v1", baseContent: "v1");

        var result = await BuildUseCase().HandleAsync(new DiffItemCommand(ProjectDirectory, "email-smtp"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.HasChanges.Should().BeTrue();
        result.Value.Files.Should().ContainSingle(f => f.Path == "SmtpEmailSender.cs" && f.Status == "UpdateAvailable");
    }

    [Fact]
    public async Task Should_ReportNoChanges_When_LocalEqualsRegistry()
    {
        Seed(registryContent: "v2", localContent: "v2", baseContent: "v1");

        var result = await BuildUseCase().HandleAsync(new DiffItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReportConflict_When_BothEdited()
    {
        Seed(registryContent: "v2", localContent: "edited", baseContent: "v1");

        var result = await BuildUseCase().HandleAsync(new DiffItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.Files.Should().ContainSingle(f => f.Status == "Conflict");
    }

    [Fact]
    public async Task Should_Fail_When_ItemNotInstalled()
    {
        _config.Seed(OutletConfig.CreateDefault("App.csproj", "MyApp"));

        var result = await BuildUseCase().HandleAsync(new DiffItemCommand(ProjectDirectory, "email-smtp"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not installed");
    }
}
