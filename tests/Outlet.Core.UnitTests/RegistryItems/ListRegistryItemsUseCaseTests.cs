using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class ListRegistryItemsUseCaseTests
{
    private readonly FakeRegistryClient _registryClient = new();
    private readonly ListRegistryItemsUseCase _useCase;

    public ListRegistryItemsUseCaseTests()
    {
        _useCase = new ListRegistryItemsUseCase(_registryClient);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoRegistryHasItems()
    {
        var result = await _useCase.HandleAsync(new ListRegistryItemsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnAllItems_When_NoConcernFilterIsGiven()
    {
        SeedEmailItems();

        var result = await _useCase.HandleAsync(new ListRegistryItemsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_FilterByConcern_When_ConcernIsGiven()
    {
        SeedEmailItems();
        Seed("cache-redis", "cache", RegistryItemType.Adapter);

        var result = await _useCase.HandleAsync(new ListRegistryItemsQuery(Concern: "email"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().OnlyContain(s => s.Concern == "email");
    }

    [Fact]
    public async Task Should_MapTypeToManifestVocabulary_When_ItemsAreReturned()
    {
        SeedEmailItems();

        var result = await _useCase.HandleAsync(new ListRegistryItemsQuery());

        var summaries = result.Value!;
        summaries.Single(s => s.Name == "email-abstractions").Type.Should().Be("outlet:contract");
        summaries.Single(s => s.Name == "email-smtp").Type.Should().Be("outlet:adapter");
    }

    private void SeedEmailItems()
    {
        Seed("email-abstractions", "email", RegistryItemType.Contract);
        Seed("email-smtp", "email", RegistryItemType.Adapter);
        Seed("email-sendgrid", "email", RegistryItemType.Adapter);
    }

    private void Seed(string id, string concern, RegistryItemType type)
    {
        var item = RegistryItem.Create(
            RegistryItemId.From(id),
            ConcernName.From(concern),
            type,
            ["File.cs"]).Value!;

        _registryClient.Seed(item);
    }
}
