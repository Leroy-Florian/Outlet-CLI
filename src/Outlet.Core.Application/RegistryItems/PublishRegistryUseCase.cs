using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.RegistryItems;

/// <summary>
/// Command: build the publishable registry artifact from the source tree at
/// <paramref name="RegistryRoot"/> into <paramref name="OutputDirectory"/>. When
/// <paramref name="DryRun"/> is true the catalogue is built and validated ("the
/// manifest never lies") but nothing is written — the CI gate that proves a registry
/// is publishable without producing the artifact.
/// </summary>
public sealed record PublishRegistryCommand(string RegistryRoot, string OutputDirectory, bool DryRun = false);

/// <summary>What a publish produced (or would produce): where it went, whether it was a preview, and each item with its file count.</summary>
public sealed record RegistryPublicationReport(
    string OutputDirectory,
    bool DryRun,
    IReadOnlyList<PublishedRegistryItem> Items);

/// <summary>One published item: its name and the number of files written under it.</summary>
public sealed record PublishedRegistryItem(string Name, int FileCount);

/// <summary>
/// Publishes a registry: delegates the build + write to <see cref="IRegistryPublisher"/>,
/// which validates every manifest before emitting the artifact. A malformed registry
/// fails the publish (the artifact is never half-written).
/// </summary>
public sealed class PublishRegistryUseCase(IRegistryPublisher publisher)
    : IUseCase<PublishRegistryCommand, RegistryPublicationReport>
{
    public Task<Result<RegistryPublicationReport>> HandleAsync(PublishRegistryCommand command, CancellationToken cancellationToken = default)
        => publisher.PublishAsync(command.RegistryRoot, command.OutputDirectory, command.DryRun, cancellationToken);
}
