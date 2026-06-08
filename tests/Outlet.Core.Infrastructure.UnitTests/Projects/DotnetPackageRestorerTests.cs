using Outlet.Core.Application.Ports;
using Outlet.Core.Infrastructure.Projects;

namespace Outlet.Core.Infrastructure.UnitTests.Projects;

/// <summary>
/// Integration test of the real restorer: it shells out to the local
/// <c>dotnet restore</c> on a bare class library (no external packages → no network,
/// the SDK targeting packs are local) and proves a clean restore reports success.
/// Hermetic but SDK-bound.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DotnetPackageRestorerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "outlet-restore-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Should_RestoreRealProject_When_GraphIsCoherent()
    {
        var project = Path.Combine(_root, "Probe.csproj");
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var result = await new DotnetPackageRestorer().RestoreAsync(new RestoreRequest(project));

        result.Succeeded.Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
