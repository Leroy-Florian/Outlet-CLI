namespace Outlet.Core.Application.Configuration;

/// <summary>
/// The project's <c>outlet.json</c> (Outlet's equivalent of shadcn's components.json):
/// the registries to pull from, how item types route into the user's projects, and
/// the lockfile of what is currently installed.
/// </summary>
public sealed record OutletConfig(
    IReadOnlyList<RegistryConfig> Registries,
    OutletTargets Targets,
    IReadOnlyList<InstalledItem> Installed)
{
    /// <summary>
    /// A fresh single-project config: one default registry and both item types
    /// routed to the same project + namespace (the mono-project default).
    /// </summary>
    public static OutletConfig CreateDefault(string projectPath, string rootNamespace)
    {
        var route = new TargetRoute(projectPath, rootNamespace);
        return new OutletConfig(
            [new RegistryConfig("outlet", "https://registry.outlet.dev/")],
            new OutletTargets(route, route),
            []);
    }
}

/// <summary>A configured registry source. <paramref name="Auth"/> is null for anonymous (public) registries.</summary>
public sealed record RegistryConfig(string Name, string Url, RegistryAuth? Auth = null);

/// <summary>
/// How the CLI authenticates to a private registry. The secret itself is NEVER stored
/// in <c>outlet.json</c>: <paramref name="TokenEnv"/> names the environment variable
/// (or CI secret) holding it. <paramref name="Scheme"/> is the HTTP auth scheme (e.g. "Bearer").
/// </summary>
public sealed record RegistryAuth(string Scheme, string TokenEnv);

/// <summary>Routing of each item type into the user's projects.</summary>
public sealed record OutletTargets(TargetRoute Contract, TargetRoute Adapter);

/// <summary>Where a routed item lands: the destination project file and root namespace.</summary>
public sealed record TargetRoute(string Project, string Namespace);

/// <summary>Lockfile entry: an installed item, its resolved version, written files, NuGet packages and registry dependencies.</summary>
public sealed record InstalledItem(
    string Name,
    string Version,
    IReadOnlyList<InstalledFile> Files,
    IReadOnlyList<InstalledPackage> Packages,
    IReadOnlyList<string> Dependencies);

/// <summary>A written file: its project-relative <paramref name="Path"/> and the content hash as originally written (to detect local edits on update/diff).</summary>
public sealed record InstalledFile(string Path, string Hash);

/// <summary>A NuGet package an installed item added, at the floor version applied.</summary>
public sealed record InstalledPackage(string Id, string Version);
