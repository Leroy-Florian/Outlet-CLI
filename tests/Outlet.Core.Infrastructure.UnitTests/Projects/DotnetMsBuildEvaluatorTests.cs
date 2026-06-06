using Outlet.Core.Infrastructure.Projects;

namespace Outlet.Core.Infrastructure.UnitTests.Projects;

/// <summary>
/// Integration test of the real evaluator: it shells out to the local
/// <c>dotnet msbuild</c> (no network — evaluation needs no restore) and proves the
/// emitted JSON matches <see cref="MsBuildOutputParser"/>. Hermetic but SDK-bound.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DotnetMsBuildEvaluatorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "outlet-eval-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Should_EvaluateRealProject_When_DotnetIsAvailable()
    {
        var project = Path.Combine(_root, "Probe.csproj");
        Directory.CreateDirectory(_root);
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RootNamespace>Acme.Probe</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Serilog" Version="3.1.1" />
              </ItemGroup>
            </Project>
            """);

        var evaluation = await new DotnetMsBuildEvaluator().EvaluateAsync(project);

        evaluation.Properties["TargetFramework"].Should().Be("net10.0");
        evaluation.Properties["RootNamespace"].Should().Be("Acme.Probe");
        evaluation.PackageReferences.Should().ContainSingle()
            .Which.Identity.Should().Be("Serilog");
        evaluation.PackageReferences[0].Metadata["Version"].Should().Be("3.1.1");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
