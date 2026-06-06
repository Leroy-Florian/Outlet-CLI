using Outlet.Core.Application.Configuration;
using Outlet.Core.Infrastructure.Configuration;
using Outlet.Core.Infrastructure.UnitTests.Fakes;

namespace Outlet.Core.Infrastructure.UnitTests.Configuration;

public sealed class JsonOutletConfigStoreTests
{
    private readonly FakeFileSystem _fileSystem = new();
    private readonly JsonOutletConfigStore _store;

    public JsonOutletConfigStoreTests()
    {
        _store = new JsonOutletConfigStore(_fileSystem);
    }

    [Fact]
    public async Task Should_SaveThenLoad_RoundTrip()
    {
        var config = OutletConfig.CreateDefault("App.csproj", "App");

        await _store.SaveAsync("/repo", config);
        var loaded = await _store.LoadAsync("/repo");

        _store.Exists("/repo").Should().BeTrue();
        loaded.IsSuccess.Should().BeTrue(loaded.Error);
        loaded.Value.Should().BeEquivalentTo(config);
    }

    [Fact]
    public async Task Should_Fail_When_ConfigIsAbsent()
    {
        var result = await _store.LoadAsync("/repo");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outlet init");
    }

    [Fact]
    public void Should_ReportNotExists_When_NoFileWritten()
    {
        _store.Exists("/repo").Should().BeFalse();
    }
}
