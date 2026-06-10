using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class PreviewInitProjectUseCaseTests
{
    private const string ProjectDirectory = "/repo";

    private readonly FakeProjectInspector _inspector = new();
    private readonly FakeOutletConfigStore _config = new();

    private PreviewInitProjectUseCase BuildUseCase() => new(_inspector, _config);

    [Fact]
    public async Task Should_ProposeHexagonalRoutes_When_MultiProject()
    {
        _inspector.WithProject("/repo/src/Acme.Application/Acme.Application.csproj", "Acme.Application", "net10.0");
        _inspector.WithProject("/repo/src/Acme.Infrastructure/Acme.Infrastructure.csproj", "Acme.Infrastructure", "net10.0");

        var result = await BuildUseCase().HandleAsync(new PreviewInitProjectQuery(ProjectDirectory));

        result.IsSuccess.Should().BeTrue(result.Error);
        var preview = result.Value!;
        preview.ConfigExists.Should().BeFalse();
        preview.IsMultiProject.Should().BeTrue();
        preview.Projects.Should().HaveCount(2);
        preview.ProposedContractProject.Should().Be(Path.Combine("src", "Acme.Application", "Acme.Application.csproj"));
        preview.ProposedAdapterProject.Should().Be(Path.Combine("src", "Acme.Infrastructure", "Acme.Infrastructure.csproj"));
    }

    [Fact]
    public async Task Should_ReportConfigExists_Without_Inspecting()
    {
        _config.Seed(OutletConfig.CreateDefault("App.csproj", "App"));

        var result = await BuildUseCase().HandleAsync(new PreviewInitProjectQuery(ProjectDirectory));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.ConfigExists.Should().BeTrue();
        result.Value!.Projects.Should().BeEmpty();
    }
}
