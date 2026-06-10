using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Application.Registries;

/// <summary>Query: list the registries configured in the project's <c>outlet.json</c>.</summary>
public sealed record ListRegistriesQuery(string ProjectDirectory);

/// <summary>Read-model DTO for CLI display: a configured registry and whether the user trusts it.</summary>
public sealed record RegistrySummary(string Name, string Url, bool Trusted);

/// <summary>Lists the configured registries (name, url, trust) so the user can see where items come from.</summary>
public sealed class ListRegistriesUseCase(IOutletConfigStore configStore)
    : IUseCase<ListRegistriesQuery, IReadOnlyList<RegistrySummary>>
{
    public async Task<Result<IReadOnlyList<RegistrySummary>>> HandleAsync(
        ListRegistriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var config = await configStore.LoadAsync(query.ProjectDirectory, cancellationToken);
        if (config.IsFailure)
            return Result<IReadOnlyList<RegistrySummary>>.Failure(config.Error!);

        IReadOnlyList<RegistrySummary> summaries =
            [.. config.Value!.Registries.Select(registry => new RegistrySummary(registry.Name, registry.Url, registry.Trusted))];

        return Result<IReadOnlyList<RegistrySummary>>.Success(summaries);
    }
}
