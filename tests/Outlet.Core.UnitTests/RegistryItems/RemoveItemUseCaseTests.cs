using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class RemoveItemUseCaseTests
{
    private const string ProjectDirectory = "/repo";

    private readonly FakeProjectInspector _inspector = new();
    private readonly FakeFileSystem _fileSystem = new();
    private readonly FakeNuGetEditor _nuGet = new();
    private readonly FakeOutletConfigStore _config = new();

    private RemoveItemUseCase BuildUseCase() => new(_inspector, _fileSystem, _nuGet, _config);

    private static InstalledItem Item(string name, string file, string[] packages, string[] dependencies)
        => new(name, "0.0.0", [new InstalledFile(file, "hash")], [.. packages.Select(p => new InstalledPackage(p, "1.0.0"))], dependencies);

    private void SeedInstalled(params InstalledItem[] items)
    {
        _config.Seed(OutletConfig.CreateDefault("App.csproj", "MyApp") with { Installed = [.. items] });
        foreach (var item in items)
            foreach (var file in item.Files)
                _fileSystem.Seed(Path.Combine(ProjectDirectory, file.Path), "// owned");
    }

    [Fact]
    public async Task Should_DeleteFiles_CleanPackage_And_UpdateLockfile()
    {
        SeedInstalled(
            Item("email-abstractions", "Email/IEmailSender.cs", [], []),
            Item("email-smtp", "Email/SmtpEmailSender.cs", ["MailKit"], ["email-abstractions"]));

        var result = await BuildUseCase().HandleAsync(new RemoveItemCommand(ProjectDirectory, "email-smtp"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.DeletedFiles.Should().Contain("Email/SmtpEmailSender.cs");
        result.Value.RemovedPackages.Should().Contain("MailKit");
        _fileSystem.Files.Should().NotContainKey("/repo/Email/SmtpEmailSender.cs");
        _config.Saved!.Installed.Select(i => i.Name).Should().Equal("email-abstractions");
        _nuGet.RemoveRequests.Should().ContainSingle().Which.PackageId.Should().Be("MailKit");
    }

    [Fact]
    public async Task Should_KeepPackage_When_StillUsedByAnotherItem()
    {
        SeedInstalled(
            Item("email-smtp", "Email/SmtpEmailSender.cs", ["MailKit"], []),
            Item("email-other", "Email/Other.cs", ["MailKit"], []));

        var result = await BuildUseCase().HandleAsync(new RemoveItemCommand(ProjectDirectory, "email-smtp"));

        result.Value!.RemovedPackages.Should().BeEmpty();
        _nuGet.RemoveRequests.Should().BeEmpty("MailKit is still used by email-other");
    }

    [Fact]
    public async Task Should_Warn_When_OtherItemsStillDependOnIt()
    {
        SeedInstalled(
            Item("email-abstractions", "Email/IEmailSender.cs", [], []),
            Item("email-smtp", "Email/SmtpEmailSender.cs", ["MailKit"], ["email-abstractions"]));

        var result = await BuildUseCase().HandleAsync(new RemoveItemCommand(ProjectDirectory, "email-abstractions"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Warnings.Should().ContainMatch("*still used by*email-smtp*");
    }

    [Fact]
    public async Task Should_Fail_When_ItemNotInstalled()
    {
        SeedInstalled(Item("email-smtp", "Email/SmtpEmailSender.cs", ["MailKit"], []));

        var result = await BuildUseCase().HandleAsync(new RemoveItemCommand(ProjectDirectory, "email-sendgrid"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not installed");
    }

    [Fact]
    public async Task Should_Fail_When_NoConfig()
    {
        var result = await BuildUseCase().HandleAsync(new RemoveItemCommand(ProjectDirectory, "email-smtp"));

        result.IsFailure.Should().BeTrue();
    }
}
