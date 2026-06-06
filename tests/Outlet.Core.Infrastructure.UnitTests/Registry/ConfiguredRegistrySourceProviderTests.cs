using Outlet.Core.Application.Configuration;
using Outlet.Core.Infrastructure.Configuration;
using Outlet.Core.Infrastructure.Registry;
using Outlet.Core.Infrastructure.UnitTests.Fakes;

namespace Outlet.Core.Infrastructure.UnitTests.Registry;

public sealed class ConfiguredRegistrySourceProviderTests
{
    private const string WorkingDirectory = "/repo";

    [Fact]
    public async Task Should_BuildOneHttpSourcePerConfiguredRegistry()
    {
        var fileSystem = new FakeFileSystem();
        var store = new JsonOutletConfigStore(fileSystem);
        var config = OutletConfig.CreateDefault("App.csproj", "App") with
        {
            Registries = [new RegistryConfig("public", "https://a.test/"), new RegistryConfig("private", "https://b.test/")],
        };
        await store.SaveAsync(WorkingDirectory, config);

        var provider = new ConfiguredRegistrySourceProvider(store, new HttpClient(), WorkingDirectory);
        var sources = await provider.GetSourcesAsync();

        sources.Should().HaveCount(2).And.AllBeOfType<HttpRegistrySource>();
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoConfigExists()
    {
        var store = new JsonOutletConfigStore(new FakeFileSystem());

        var provider = new ConfiguredRegistrySourceProvider(store, new HttpClient(), WorkingDirectory);
        var sources = await provider.GetSourcesAsync();

        sources.Should().BeEmpty();
    }
}
