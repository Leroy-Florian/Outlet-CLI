using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Registries;

/// <summary>
/// Command: add a registry source to the project's <c>outlet.json</c>. A newly added registry is
/// untrusted by default — the user must vouch for it (<c>--trusted</c> here, or <c>outlet registry
/// trust</c> later) before items from it can be installed without <c>--yes</c>.
/// </summary>
public sealed record AddRegistryCommand(string ProjectDirectory, string Name, string Url, bool Trusted = false);

/// <summary>
/// Appends a registry to the config after validating its name and absolute URL, and that it does
/// not collide with an existing one. Returns the registry that was added.
/// </summary>
public sealed class AddRegistryUseCase(IOutletConfigStore configStore)
    : IUseCase<AddRegistryCommand, RegistryConfig>
{
    public async Task<Result<RegistryConfig>> HandleAsync(AddRegistryCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<RegistryConfig>.Failure("a registry name is required.");

        if (!Uri.TryCreate(command.Url, UriKind.Absolute, out _))
            return Result<RegistryConfig>.Failure($"registry url '{command.Url}' must be an absolute URL.");

        var configResult = await configStore.LoadAsync(command.ProjectDirectory, cancellationToken);
        if (configResult.IsFailure)
            return Result<RegistryConfig>.Failure(configResult.Error!);
        var config = configResult.Value!;

        if (config.Registries.Any(registry => string.Equals(registry.Name, command.Name, StringComparison.Ordinal)))
            return Result<RegistryConfig>.Failure($"a registry named '{command.Name}' is already configured.");

        var added = new RegistryConfig(command.Name, command.Url, command.Trusted);
        var updated = config with { Registries = [.. config.Registries, added] };
        await configStore.SaveAsync(command.ProjectDirectory, updated, cancellationToken);

        return Result<RegistryConfig>.Success(added);
    }
}
