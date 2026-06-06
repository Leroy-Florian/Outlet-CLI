using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.Registry;
using Outlet.Core.Infrastructure.UnitTests.Fakes;

namespace Outlet.Core.Infrastructure.UnitTests.Registry;

public sealed class MultiSourceRegistryClientTests
{
    [Fact]
    public async Task Should_AggregateItemsAcrossSources()
    {
        var publicSource = new FakeRegistrySource().Add(Item("email-smtp"));
        var privateSource = new FakeRegistrySource().Add(Item("email-internal"));
        var client = new MultiSourceRegistryClient([publicSource, privateSource]);

        var items = await client.GetItemsAsync();

        items.Select(i => i.Id.Value).Should().BeEquivalentTo(["email-smtp", "email-internal"]);
    }

    [Fact]
    public async Task Should_LetFirstSourceWin_When_ItemNameCollides()
    {
        var primary = new FakeRegistrySource().Add(Item("email-smtp", "email"));
        var secondary = new FakeRegistrySource().Add(Item("email-smtp", "messaging"));
        var client = new MultiSourceRegistryClient([primary, secondary]);

        var items = await client.GetItemsAsync();

        items.Should().ContainSingle().Which.Concern.Value.Should().Be("email");
    }

    [Fact]
    public async Task Should_ReturnFirstMatch_When_GettingItemById()
    {
        var primary = new FakeRegistrySource();
        var secondary = new FakeRegistrySource().Add(Item("email-smtp"));
        var client = new MultiSourceRegistryClient([primary, secondary]);

        var item = await client.GetItemAsync(RegistryItemId.From("email-smtp"));

        item.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_ReturnNull_When_NoSourceHasTheItem()
    {
        var client = new MultiSourceRegistryClient([new FakeRegistrySource()]);

        var item = await client.GetItemAsync(RegistryItemId.From("email-smtp"));

        item.Should().BeNull();
    }

    [Fact]
    public async Task Should_RouteFileFetchToTheOwningSource()
    {
        var primary = new FakeRegistrySource();
        var secondary = new FakeRegistrySource()
            .Add(Item("email-smtp"))
            .AddFile("email-smtp", "SmtpEmailSender.cs", "owned");
        var client = new MultiSourceRegistryClient([primary, secondary]);

        var content = await client.GetFileContentAsync(RegistryItemId.From("email-smtp"), "SmtpEmailSender.cs");

        content.Should().Be("owned");
    }

    [Fact]
    public async Task Should_Throw_When_FetchingFileOfUnknownItem()
    {
        var client = new MultiSourceRegistryClient([new FakeRegistrySource()]);

        var act = async () => await client.GetFileContentAsync(RegistryItemId.From("email-smtp"), "X.cs");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static RegistryItem Item(string id, string concern = "email")
        => RegistryItem.Create(
            RegistryItemId.From(id),
            ConcernName.From(concern),
            RegistryItemType.Adapter,
            ["File.cs"]).Value!;
}
