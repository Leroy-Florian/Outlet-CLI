using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Registries;

/// <summary>
/// Command: mark the registry named <paramref name="Name"/> as trusted (or not). Trusting a
/// registry is the deliberate act of vouching for the code it serves — afterwards its items
/// install without <c>--yes</c>; untrusting reinstates the gate.
/// </summary>
public sealed record SetRegistryTrustCommand(string ProjectDirectory, string Name, bool Trusted);

/// <summary>Flips a configured registry's trust flag, failing if no registry by that name exists.</summary>
public sealed class SetRegistryTrustUseCase(IOutletConfigStore configStore)
    : IUseCase<SetRegistryTrustCommand, RegistryConfig>
{
    public async Task<Result<RegistryConfig>> HandleAsync(SetRegistryTrustCommand command, CancellationToken cancellationToken = default)
    {
        var configResult = await configStore.LoadAsync(command.ProjectDirectory, cancellationToken);
        if (configResult.IsFailure)
            return Result<RegistryConfig>.Failure(configResult.Error!);
        var config = configResult.Value!;

        var existing = config.Registries.FirstOrDefault(registry => string.Equals(registry.Name, command.Name, StringComparison.Ordinal));
        if (existing is null)
            return Result<RegistryConfig>.Failure($"no registry named '{command.Name}' is configured.");

        var updated = existing with { Trusted = command.Trusted };
        var config2 = config with
        {
            Registries = [.. config.Registries.Select(registry =>
                string.Equals(registry.Name, command.Name, StringComparison.Ordinal) ? updated : registry)],
        };
        await configStore.SaveAsync(command.ProjectDirectory, config2, cancellationToken);

        return Result<RegistryConfig>.Success(updated);
    }
}
