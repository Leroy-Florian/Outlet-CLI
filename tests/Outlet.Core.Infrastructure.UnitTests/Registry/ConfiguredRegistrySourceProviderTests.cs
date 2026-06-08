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

        var provider = new ConfiguredRegistrySourceProvider(store, new HttpClient(), new EnvironmentCredentialResolver(), WorkingDirectory);
        var sources = await provider.GetSourcesAsync();

        sources.Should().HaveCount(2).And.AllBeOfType<HttpRegistrySource>();
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoConfigExists()
    {
        var store = new JsonOutletConfigStore(new FakeFileSystem());

        var provider = new ConfiguredRegistrySourceProvider(store, new HttpClient(), new EnvironmentCredentialResolver(), WorkingDirectory);
        var sources = await provider.GetSourcesAsync();

        sources.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_AttachResolvedBearerToken_When_RegistryDeclaresAuth()
    {
        Environment.SetEnvironmentVariable("OUTLET_PROVIDER_TEST_TOKEN", "secret-123");
        try
        {
            var store = new JsonOutletConfigStore(new FakeFileSystem());
            var config = OutletConfig.CreateDefault("App.csproj", "App") with
            {
                Registries = [new RegistryConfig("private", "https://b.test/", new RegistryAuth("Bearer", "OUTLET_PROVIDER_TEST_TOKEN"))],
            };
            await store.SaveAsync(WorkingDirectory, config);

            var handler = new StubHttpMessageHandler().Map("https://b.test/registry.json", """{ "items": [] }""");
            var provider = new ConfiguredRegistrySourceProvider(
                store, new HttpClient(handler), new EnvironmentCredentialResolver(), WorkingDirectory);

            var sources = await provider.GetSourcesAsync();
            await sources.Single().GetItemsAsync();

            handler.LastAuthorization.Should().NotBeNull();
            handler.LastAuthorization!.Scheme.Should().Be("Bearer");
            handler.LastAuthorization.Parameter.Should().Be("secret-123");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OUTLET_PROVIDER_TEST_TOKEN", null);
        }
    }
}
