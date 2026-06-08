using Outlet.Core.Application.Configuration;

namespace Outlet.Core.Infrastructure.Registry;

/// <summary>
/// SECONDARY ADAPTER — resolves a registry token from an environment variable
/// (the common CI/dev mechanism: <c>ACME_OUTLET_TOKEN=…</c>). A keychain / file
/// source can be layered behind the same port later.
/// </summary>
public sealed class EnvironmentCredentialResolver : ICredentialResolver
{
    public string? Resolve(RegistryAuth auth)
    {
        var token = Environment.GetEnvironmentVariable(auth.TokenEnv);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
