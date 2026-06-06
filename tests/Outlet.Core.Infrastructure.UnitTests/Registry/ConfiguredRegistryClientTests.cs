using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.Registry;
using Outlet.Core.Infrastructure.UnitTests.Fakes;

namespace Outlet.Core.Infrastructure.UnitTests.Registry;

public sealed class ConfiguredRegistryClientTests
{
    [Fact]
    public async Task Should_AggregateItems_FromProvidedSources()
    {
        var provider = new FakeRegistrySourceProvider(
            new FakeRegistrySource().Add(Item("email-smtp")),
            new FakeRegistrySource().Add(Item("email-internal")));
        var client = new ConfiguredRegistryClient(provider);

        var items = await client.GetItemsAsync();

        items.Select(i => i.Id.Value).Should().BeEquivalentTo(["email-smtp", "email-internal"]);
    }

    [Fact]
    public async Task Should_RouteFileFetch_ToOwningSource()
    {
        var provider = new FakeRegistrySourceProvider(
            new FakeRegistrySource().Add(Item("email-smtp")).AddFile("email-smtp", "X.cs", "owned"));
        var client = new ConfiguredRegistryClient(provider);

        var content = await client.GetFileContentAsync(RegistryItemId.From("email-smtp"), "X.cs");

        content.Should().Be("owned");
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoSourcesAreConfigured()
    {
        var client = new ConfiguredRegistryClient(new FakeRegistrySourceProvider());

        (await client.GetItemsAsync()).Should().BeEmpty();
    }

    private static RegistryItem Item(string id)
        => RegistryItem.Create(
            RegistryItemId.From(id),
            ConcernName.From("email"),
            RegistryItemType.Adapter,
            ["File.cs"]).Value!;
}
