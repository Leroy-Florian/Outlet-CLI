using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class InitProjectUseCaseTests
{
    private const string ProjectDirectory = "/repo";

    private readonly FakeProjectInspector _inspector = new();
    private readonly FakeOutletConfigStore _config = new();

    private InitProjectUseCase BuildUseCase() => new(_inspector, _config);

    [Fact]
    public async Task Should_CreateDefaultConfig_When_SingleProjectAndNoExistingConfig()
    {
        _inspector.WithProject("/repo/src/App/App.csproj", "Acme.App", "net10.0");

        var result = await BuildUseCase().HandleAsync(new InitProjectCommand(ProjectDirectory));

        result.IsSuccess.Should().BeTrue(result.Error);
        var config = result.Value!.Config;
        config.Targets.Adapter.Project.Should().Be(Path.Combine("src", "App", "App.csproj"));
        config.Targets.Adapter.Namespace.Should().Be("Acme.App");
        config.Targets.Contract.Project.Should().Be(config.Targets.Adapter.Project);
        result.Value!.HexagonalRoutingApplied.Should().BeFalse();
        _config.Saved.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_RouteContractAndAdapterToSeparateProjects_When_HexagonalLayout()
    {
        _inspector.WithProject("/repo/src/Acme.Domain/Acme.Domain.csproj", "Acme.Domain", "net10.0");
        _inspector.WithProject("/repo/src/Acme.Application/Acme.Application.csproj", "Acme.Application", "net10.0");
        _inspector.WithProject("/repo/src/Acme.Infrastructure/Acme.Infrastructure.csproj", "Acme.Infrastructure", "net10.0");
        _inspector.WithProject("/repo/src/Acme.Api/Acme.Api.csproj", "Acme.Api", "net10.0");

        var result = await BuildUseCase().HandleAsync(new InitProjectCommand(ProjectDirectory));

        result.IsSuccess.Should().BeTrue(result.Error);
        var report = result.Value!;
        report.HexagonalRoutingApplied.Should().BeTrue();
        report.Config.Targets.Contract.Project.Should().Be(Path.Combine("src", "Acme.Application", "Acme.Application.csproj"));
        report.Config.Targets.Adapter.Project.Should().Be(Path.Combine("src", "Acme.Infrastructure", "Acme.Infrastructure.csproj"));
    }

    [Fact]
    public async Task Should_FallBackToDomain_When_NoApplicationProject()
    {
        _inspector.WithProject("/repo/src/Acme.Domain/Acme.Domain.csproj", "Acme.Domain", "net10.0");
        _inspector.WithProject("/repo/src/Acme.Infrastructure/Acme.Infrastructure.csproj", "Acme.Infrastructure", "net10.0");

        var result = await BuildUseCase().HandleAsync(new InitProjectCommand(ProjectDirectory));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Config.Targets.Contract.Project
            .Should().Be(Path.Combine("src", "Acme.Domain", "Acme.Domain.csproj"));
    }

    [Fact]
    public async Task Should_UseExplicitRoutes_When_OverridesProvided()
    {
        _inspector.WithProject("/repo/src/Acme.Application/Acme.Application.csproj", "Acme.Application", "net10.0");
        _inspector.WithProject("/repo/src/Acme.Infrastructure/Acme.Infrastructure.csproj", "Acme.Infrastructure", "net10.0");
        _inspector.WithProject("/repo/src/Acme.Api/Acme.Api.csproj", "Acme.Api", "net10.0");

        var contractOverride = Path.Combine("src", "Acme.Api", "Acme.Api.csproj");
        var adapterOverride = Path.Combine("src", "Acme.Infrastructure", "Acme.Infrastructure.csproj");

        var result = await BuildUseCase().HandleAsync(
            new InitProjectCommand(ProjectDirectory, contractOverride, adapterOverride));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Config.Targets.Contract.Project.Should().Be(contractOverride);
        result.Value!.Config.Targets.Adapter.Project.Should().Be(adapterOverride);
    }

    [Fact]
    public async Task Should_Fail_When_OverrideProjectUnknown()
    {
        _inspector.WithProject("/repo/src/App/App.csproj", "Acme.App", "net10.0");

        var result = await BuildUseCase().HandleAsync(
            new InitProjectCommand(ProjectDirectory, ContractProject: "does/not/Exist.csproj"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Unknown project");
    }

    [Fact]
    public async Task Should_Fail_When_ConfigAlreadyExists()
    {
        _inspector.WithProject("/repo/App.csproj", "App");
        _config.Seed(OutletConfig.CreateDefault("App.csproj", "App"));

        var result = await BuildUseCase().HandleAsync(new InitProjectCommand(ProjectDirectory));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Should_Fail_When_NoProjectFound()
    {
        var result = await BuildUseCase().HandleAsync(new InitProjectCommand(ProjectDirectory));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No .csproj");
    }
}
