using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class SearchRegistryItemsUseCaseTests
{
    private readonly FakeRegistryClient _registryClient = new();
    private readonly SearchRegistryItemsUseCase _useCase;

    public SearchRegistryItemsUseCaseTests()
    {
        _useCase = new SearchRegistryItemsUseCase(_registryClient);
        Seed("email-abstractions", "email", RegistryItemType.Contract);
        Seed("email-smtp", "email", RegistryItemType.Adapter);
        Seed("sms-twilio", "sms", RegistryItemType.Adapter);
        Seed("cache-redis", "cache", RegistryItemType.Adapter);
    }

    [Fact]
    public async Task Should_MatchOnItemId()
    {
        var result = await _useCase.HandleAsync(new SearchRegistryItemsQuery("smtp"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Select(s => s.Name).Should().Equal("email-smtp");
    }

    [Fact]
    public async Task Should_MatchOnConcern_AcrossAdaptersAndContract()
    {
        var result = await _useCase.HandleAsync(new SearchRegistryItemsQuery("email"));

        result.Value!.Select(s => s.Name).Should().Equal("email-abstractions", "email-smtp");
    }

    [Fact]
    public async Task Should_BeCaseInsensitive()
    {
        var result = await _useCase.HandleAsync(new SearchRegistryItemsQuery("REDIS"));

        result.Value!.Select(s => s.Name).Should().Equal("cache-redis");
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NothingMatches()
    {
        var result = await _useCase.HandleAsync(new SearchRegistryItemsQuery("kafka"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnAllItemsSorted_When_TextIsBlank()
    {
        var result = await _useCase.HandleAsync(new SearchRegistryItemsQuery("   "));

        result.Value!.Select(s => s.Name)
            .Should().Equal("cache-redis", "email-abstractions", "email-smtp", "sms-twilio");
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
