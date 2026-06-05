using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class ResolveItemDependenciesUseCaseTests
{
    private readonly FakeRegistryClient _registryClient = new();
    private readonly ResolveItemDependenciesUseCase _useCase;

    public ResolveItemDependenciesUseCaseTests()
    {
        _useCase = new ResolveItemDependenciesUseCase(_registryClient);
    }

    [Fact]
    public async Task Should_ReturnItemAlone_When_ItHasNoDependencies()
    {
        Seed("email-abstractions");

        var result = await _useCase.HandleAsync(new ResolveItemDependenciesQuery("email-abstractions"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Select(i => i.Id.Value).Should().Equal("email-abstractions");
    }

    [Fact]
    public async Task Should_ReturnDependenciesFirst_When_AdapterDependsOnContract()
    {
        Seed("email-abstractions");
        Seed("email-smtp", "email-abstractions");

        var result = await _useCase.HandleAsync(new ResolveItemDependenciesQuery("email-smtp"));

        result.IsSuccess.Should().BeTrue(result.Error);
        result.Value!.Select(i => i.Id.Value).Should().Equal("email-abstractions", "email-smtp");
    }

    [Fact]
    public async Task Should_DeduplicateSharedDependency_When_GraphIsADiamond()
    {
        Seed("a");
        Seed("b", "a");
        Seed("c", "a");
        Seed("d", "b", "c");

        var result = await _useCase.HandleAsync(new ResolveItemDependenciesQuery("d"));

        result.IsSuccess.Should().BeTrue(result.Error);
        var order = result.Value!.Select(i => i.Id.Value).ToList();
        order.Should().HaveCount(4);
        order.Should().ContainSingle(name => name == "a");
        order.IndexOf("a").Should().BeLessThan(order.IndexOf("b"));
        order.IndexOf("a").Should().BeLessThan(order.IndexOf("c"));
        order.IndexOf("b").Should().BeLessThan(order.IndexOf("d"));
        order.IndexOf("c").Should().BeLessThan(order.IndexOf("d"));
    }

    [Fact]
    public async Task Should_Fail_When_DependencyGraphHasACycle()
    {
        Seed("a", "b");
        Seed("b", "a");

        var result = await _useCase.HandleAsync(new ResolveItemDependenciesQuery("a"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cyclic");
    }

    [Fact]
    public async Task Should_Fail_When_RootItemDoesNotExist()
    {
        var result = await _useCase.HandleAsync(new ResolveItemDependenciesQuery("missing"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("was not found");
    }

    [Fact]
    public async Task Should_Fail_When_ADependencyIsMissing()
    {
        Seed("email-smtp", "email-abstractions");

        var result = await _useCase.HandleAsync(new ResolveItemDependenciesQuery("email-smtp"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("email-abstractions");
    }

    [Fact]
    public async Task Should_Fail_When_ItemNameIsNotKebabCase()
    {
        var result = await _useCase.HandleAsync(new ResolveItemDependenciesQuery("Email_SMTP"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid item name");
    }

    private void Seed(string id, params string[] registryDependencies)
    {
        var type = registryDependencies.Length == 0 && id.Contains("abstractions")
            ? RegistryItemType.Contract
            : RegistryItemType.Adapter;

        var item = RegistryItem.Create(
            RegistryItemId.From(id),
            ConcernName.From("email"),
            type,
            ["File.cs"],
            [.. registryDependencies.Select(RegistryItemId.From)]).Value!;

        _registryClient.Seed(item);
    }
}
