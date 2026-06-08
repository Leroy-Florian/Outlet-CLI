using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Cli;

/// <summary>
/// Level 1 of the auto-update story: explicitly update the CLI on demand
/// (<c>outlet self-update</c>). Thin wrapper over the <see cref="ICliUpdater"/>
/// port — a non-zero tool exit becomes a business failure, never an exception.
/// </summary>
public sealed class SelfUpdateCliUseCase(ICliUpdater updater)
    : IUseCase<SelfUpdateCliCommand, string>
{
    public async Task<Result<string>> HandleAsync(
        SelfUpdateCliCommand command,
        CancellationToken cancellationToken = default)
    {
        var outcome = await updater.UpdateAsync(cancellationToken);

        return outcome.Succeeded
            ? Result<string>.Success(outcome.Detail)
            : Result<string>.Failure(outcome.Detail);
    }
}
