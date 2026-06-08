using Outlet.Core.Application.Configuration;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// Resolves the secret token for a registry's <see cref="RegistryAuth"/> from a
/// trusted local source. Kept out of <c>outlet.json</c> so secrets never land in
/// version control. Returns null when the secret is not present (the request then
/// goes out unauthenticated and the registry answers 401).
/// </summary>
public interface ICredentialResolver
{
    string? Resolve(RegistryAuth auth);
}
