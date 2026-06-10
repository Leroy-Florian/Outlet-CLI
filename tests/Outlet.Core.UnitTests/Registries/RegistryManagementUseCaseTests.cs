using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Registries;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.Registries;

public sealed class RegistryManagementUseCaseTests
{
    private const string ProjectDirectory = "/repo";

    private readonly FakeOutletConfigStore _config = new();

    public RegistryManagementUseCaseTests()
        => _config.Seed(OutletConfig.CreateDefault("App.csproj", "MyApp"));

    [Fact]
    public async Task Should_AddRegistryUntrusted_ByDefault()
    {
        var result = await new AddRegistryUseCase(_config).HandleAsync(
            new AddRegistryCommand(ProjectDirectory, "corp", "https://corp.example/registry/"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Trusted.Should().BeFalse();
        _config.Saved!.Registries.Should().Contain(r => r.Name == "corp" && !r.Trusted);
    }

    [Fact]
    public async Task Should_AddRegistryTrusted_When_Requested()
    {
        var result = await new AddRegistryUseCase(_config).HandleAsync(
            new AddRegistryCommand(ProjectDirectory, "corp", "https://corp.example/", Trusted: true));

        result.Value!.Trusted.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_AddingRegistryWithNonAbsoluteUrl()
    {
        var result = await new AddRegistryUseCase(_config).HandleAsync(
            new AddRegistryCommand(ProjectDirectory, "corp", "registry.json"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("absolute");
    }

    [Fact]
    public async Task Should_Fail_When_AddingDuplicateRegistryName()
    {
        var result = await new AddRegistryUseCase(_config).HandleAsync(
            new AddRegistryCommand(ProjectDirectory, "outlet", "https://other.example/"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already configured");
    }

    [Fact]
    public async Task Should_RemoveRegistry_When_ItExists()
    {
        var result = await new RemoveRegistryUseCase(_config).HandleAsync(
            new RemoveRegistryCommand(ProjectDirectory, "outlet"));

        result.IsSuccess.Should().BeTrue(result.Error);
        _config.Saved!.Registries.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_RemovingUnknownRegistry()
    {
        var result = await new RemoveRegistryUseCase(_config).HandleAsync(
            new RemoveRegistryCommand(ProjectDirectory, "ghost"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no registry named 'ghost'");
    }

    [Fact]
    public async Task Should_FlipTrust_When_TrustingAndUntrusting()
    {
        await new AddRegistryUseCase(_config).HandleAsync(
            new AddRegistryCommand(ProjectDirectory, "corp", "https://corp.example/"));

        var trusted = await new SetRegistryTrustUseCase(_config).HandleAsync(
            new SetRegistryTrustCommand(ProjectDirectory, "corp", Trusted: true));
        trusted.Value!.Trusted.Should().BeTrue();
        _config.Saved!.Registries.Single(r => r.Name == "corp").Trusted.Should().BeTrue();

        var untrusted = await new SetRegistryTrustUseCase(_config).HandleAsync(
            new SetRegistryTrustCommand(ProjectDirectory, "corp", Trusted: false));
        untrusted.Value!.Trusted.Should().BeFalse();
        _config.Saved!.Registries.Single(r => r.Name == "corp").Trusted.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Fail_When_TrustingUnknownRegistry()
    {
        var result = await new SetRegistryTrustUseCase(_config).HandleAsync(
            new SetRegistryTrustCommand(ProjectDirectory, "ghost", Trusted: true));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no registry named 'ghost'");
    }

    [Fact]
    public async Task Should_ListConfiguredRegistries_WithTrustState()
    {
        await new AddRegistryUseCase(_config).HandleAsync(
            new AddRegistryCommand(ProjectDirectory, "corp", "https://corp.example/", Trusted: true));

        var result = await new ListRegistriesUseCase(_config).HandleAsync(new ListRegistriesQuery(ProjectDirectory));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Should().Contain(r => r.Name == "outlet" && r.Trusted);
        result.Value.Should().Contain(r => r.Name == "corp" && r.Trusted);
    }
}
