using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class AddItemUseCaseTests
{
    private const string ProjectDirectory = "/repo";

    private readonly FakeRegistryClient _registry = new();
    private readonly FakeProjectInspector _inspector = new();
    private readonly FakeFileSystem _fileSystem = new();
    private readonly FakeNuGetEditor _nuGet = new();
    private readonly FakeOutletConfigStore _config = new();

    public AddItemUseCaseTests()
    {
        SeedRegistry();
        _config.Seed(OutletConfig.CreateDefault("App.csproj", "MyApp"));
    }

    private AddItemUseCase BuildUseCase()
        => new(_registry, _inspector, new FakeNamespaceRewriter(), _fileSystem, _nuGet, _config);

    [Fact]
    public async Task Should_InstallDependenciesFirst_AndWriteRewrittenFiles()
    {
        var result = await BuildUseCase().HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.InstalledItems.Should().Equal("email-abstractions", "email-smtp");
        result.Value.WrittenFiles.Should().Contain(["IEmailSender.cs", "SmtpEmailSender.cs"]);
        _fileSystem.Files["/repo/SmtpEmailSender.cs"].Should().Contain("namespace MyApp")
            .And.NotContain("Outlet.Registry.Email");
    }

    [Fact]
    public async Task Should_AddNuGetPackagesOfAdapter_ToTargetProject()
    {
        await BuildUseCase().HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        _nuGet.Requests.Should().ContainSingle();
        _nuGet.Requests[0].Dependency.PackageId.Should().Be("MailKit");
        _nuGet.Requests[0].ProjectFilePath.Should().Be(Path.Combine(ProjectDirectory, "App.csproj"));
    }

    [Fact]
    public async Task Should_RecordLockfileEntries_AfterInstall()
    {
        await BuildUseCase().HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        _config.Saved!.Installed.Select(i => i.Name).Should().Equal("email-abstractions", "email-smtp");
        var smtp = _config.Saved.Installed.Single(i => i.Name == "email-smtp");
        smtp.Files.Select(f => f.Path).Should().Contain("SmtpEmailSender.cs");
        smtp.Packages.Should().ContainSingle().Which.Id.Should().Be("MailKit");
    }

    [Fact]
    public async Task Should_SkipAlreadyInstalledDependency()
    {
        var seeded = OutletConfig.CreateDefault("App.csproj", "MyApp") with
        {
            Installed = [new InstalledItem("email-abstractions", "0.0.0", [], [], [])],
        };
        _config.Seed(seeded);

        var result = await BuildUseCase().HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.InstalledItems.Should().Equal("email-smtp");
        _fileSystem.Files.Should().NotContainKey("/repo/IEmailSender.cs");
    }

    [Fact]
    public async Task Should_WarnAndNotOverwrite_When_FileCollides()
    {
        _fileSystem.Seed("/repo/SmtpEmailSender.cs", "EXISTING USER CODE");

        var result = await BuildUseCase().HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.Warnings.Should().ContainMatch("*already exists*");
        _fileSystem.Files["/repo/SmtpEmailSender.cs"].Should().Be("EXISTING USER CODE");
    }

    [Fact]
    public async Task Should_SurfaceNuGetConflictWarning()
    {
        _nuGet.ReturningConflict("MailKit conflict: below floor");

        var result = await BuildUseCase().HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.Warnings.Should().Contain("MailKit conflict: below floor");
    }

    [Fact]
    public async Task Should_Fail_When_ConfigIsMissing()
    {
        var emptyConfig = new FakeOutletConfigStore();
        var useCase = new AddItemUseCase(_registry, _inspector, new FakeNamespaceRewriter(), _fileSystem, _nuGet, emptyConfig);

        var result = await useCase.HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outlet init");
    }

    [Fact]
    public async Task Should_Fail_When_ItemDoesNotExist()
    {
        var result = await BuildUseCase().HandleAsync(new AddItemCommand(ProjectDirectory, "email-unknown"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("was not found");
    }

    private void SeedRegistry()
    {
        SeedItem("email-abstractions", RegistryItemType.Contract, "IEmailSender.cs", []);
        SeedItem("email-smtp", RegistryItemType.Adapter, "SmtpEmailSender.cs", ["email-abstractions"], ("MailKit", "4.16.0"));
    }

    private void SeedItem(
        string id,
        RegistryItemType type,
        string file,
        string[] registryDependencies,
        params (string Id, string Version)[] packages)
    {
        var item = RegistryItem.Create(
            RegistryItemId.From(id),
            ConcernName.From("email"),
            type,
            [file],
            [.. registryDependencies.Select(RegistryItemId.From)],
            [.. packages.Select(p => PackageDependency.From(p.Id, p.Version))],
            ["net8.0", "net9.0", "net10.0"]).Value!;

        _registry.Seed(item);
        _registry.SeedFileContent(RegistryItemId.From(id), file, $"namespace Outlet.Registry.Email;\npublic sealed class Marker;\n");
    }

    [Fact]
    public async Task Should_RefuseInstall_When_ItemIsIncompatibleWithProjectTfm()
    {
        var inspector = new FakeProjectInspector().WithProject(Path.Combine(ProjectDirectory, "App.csproj"), "MyApp", "net6.0");
        var useCase = new AddItemUseCase(_registry, inspector, new FakeNamespaceRewriter(), _fileSystem, _nuGet, _config);

        var result = await useCase.HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("net6.0");
        _fileSystem.Files.Should().BeEmpty("an incompatible item must be refused before any file is written");
    }

    [Fact]
    public async Task Should_Install_When_ItemIsCompatibleWithProjectTfm()
    {
        var inspector = new FakeProjectInspector().WithProject(Path.Combine(ProjectDirectory, "App.csproj"), "MyApp", "net10.0");
        var useCase = new AddItemUseCase(_registry, inspector, new FakeNamespaceRewriter(), _fileSystem, _nuGet, _config);

        var result = await useCase.HandleAsync(new AddItemCommand(ProjectDirectory, "email-smtp"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.InstalledItems.Should().Equal("email-abstractions", "email-smtp");
    }
}
