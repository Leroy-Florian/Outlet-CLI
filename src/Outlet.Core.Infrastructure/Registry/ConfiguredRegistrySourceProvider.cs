using System.Net.Http.Headers;
using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// SECONDARY ADAPTER — builds an <see cref="HttpRegistrySource"/> per registry declared
/// in the project's <c>outlet.json</c>, attaching credentials for any registry that
/// declares <c>auth</c> (resolved via <see cref="ICredentialResolver"/>). When no config
/// exists (e.g. before <c>outlet init</c>), the source list is empty so <c>list</c>
/// degrades gracefully.
/// </summary>
public sealed class ConfiguredRegistrySourceProvider(
    IOutletConfigStore configStore,
    HttpClient httpClient,
    ICredentialResolver credentialResolver,
    string workingDirectory) : IRegistrySourceProvider
{
    public async Task<IReadOnlyList<IRegistrySource>> GetSourcesAsync(CancellationToken cancellationToken = default)
    {
        if (!configStore.Exists(workingDirectory))
            return [];

        var config = await configStore.LoadAsync(workingDirectory, cancellationToken);
        if (config.IsFailure)
            return [];

        return
        [
            .. config.Value!.Registries.Select(registry =>
                (IRegistrySource)new HttpRegistrySource(httpClient, new Uri(registry.Url), AuthorizationFor(registry))),
        ];
    }

    private AuthenticationHeaderValue? AuthorizationFor(RegistryConfig registry)
    {
        if (registry.Auth is not { } auth)
            return null;

        var token = credentialResolver.Resolve(auth);
        return token is null ? null : new AuthenticationHeaderValue(auth.Scheme, token);
    }
}
