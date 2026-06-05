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
        var config = result.Value!;
        config.Targets.Adapter.Project.Should().Be(Path.Combine("src", "App", "App.csproj"));
        config.Targets.Adapter.Namespace.Should().Be("Acme.App");
        _config.Saved.Should().NotBeNull();
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
