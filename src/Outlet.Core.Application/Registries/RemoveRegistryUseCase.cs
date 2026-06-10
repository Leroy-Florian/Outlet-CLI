using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Registries;

/// <summary>Command: remove the registry named <paramref name="Name"/> from the project's <c>outlet.json</c>.</summary>
public sealed record RemoveRegistryCommand(string ProjectDirectory, string Name);

/// <summary>Removes a configured registry, failing if no registry by that name exists. Returns the removed registry.</summary>
public sealed class RemoveRegistryUseCase(IOutletConfigStore configStore)
    : IUseCase<RemoveRegistryCommand, RegistryConfig>
{
    public async Task<Result<RegistryConfig>> HandleAsync(RemoveRegistryCommand command, CancellationToken cancellationToken = default)
    {
        var configResult = await configStore.LoadAsync(command.ProjectDirectory, cancellationToken);
        if (configResult.IsFailure)
            return Result<RegistryConfig>.Failure(configResult.Error!);
        var config = configResult.Value!;

        var removed = config.Registries.FirstOrDefault(registry => string.Equals(registry.Name, command.Name, StringComparison.Ordinal));
        if (removed is null)
            return Result<RegistryConfig>.Failure($"no registry named '{command.Name}' is configured.");

        var updated = config with
        {
            Registries = [.. config.Registries.Where(registry => !string.Equals(registry.Name, command.Name, StringComparison.Ordinal))],
        };
        await configStore.SaveAsync(command.ProjectDirectory, updated, cancellationToken);

        return Result<RegistryConfig>.Success(removed);
    }
}
