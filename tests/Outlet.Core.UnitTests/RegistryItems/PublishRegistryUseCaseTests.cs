using Outlet.Core.Application.RegistryItems;
using Outlet.Core.UnitTests.Fakes;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class PublishRegistryUseCaseTests
{
    private readonly FakeRegistryPublisher _publisher = new();

    private PublishRegistryUseCase BuildUseCase() => new(_publisher);

    [Fact]
    public async Task Should_ForwardRootOutputAndDryRun_ToThePublisher()
    {
        var result = await BuildUseCase().HandleAsync(
            new PublishRegistryCommand("registry", "dist/registry", DryRun: true));

        result.IsSuccess.Should().BeTrue(result.Error);
        _publisher.RegistryRoot.Should().Be("registry");
        _publisher.OutputDirectory.Should().Be("dist/registry");
        _publisher.DryRun.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle().Which.Name.Should().Be("email-smtp");
    }

    [Fact]
    public async Task Should_PropagateFailure_When_PublisherFails()
    {
        _publisher.Failing("Item 'ghost' declares file 'Ghost.cs' that is missing on disk.");

        var result = await BuildUseCase().HandleAsync(
            new PublishRegistryCommand("registry", "dist/registry"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("missing on disk");
    }
}
