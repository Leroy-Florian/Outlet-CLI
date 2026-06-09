using Microsoft.Extensions.DependencyInjection;
using Outlet.Core.Application.Ports;
using Outlet.Core.Infrastructure.Configuration;
using Outlet.Core.Infrastructure.Io;
using Outlet.Core.Infrastructure.NuGet;
using Outlet.Core.Infrastructure.Projects;
using Outlet.Core.Infrastructure.Registry;
using Outlet.Core.Infrastructure.Rewriting;

namespace Outlet.Core.Infrastructure.DependencyInjection;

/// <summary>
/// Explicit DI registration of the secondary adapters — no assembly scanning,
/// per the Outlet design principles.
/// </summary>
public static class OutletCoreServiceCollectionExtensions
{
    public static IServiceCollection AddOutletCoreInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<HttpClient>(_ => new HttpClient());

        // Registry sources are read from the project's outlet.json (per working
        // directory), then the client fans out across them. Private registries
        // attach a bearer credential resolved from the environment.
        services.AddSingleton<ICredentialResolver, EnvironmentCredentialResolver>();
        services.AddScoped<IRegistrySourceProvider>(sp => new ConfiguredRegistrySourceProvider(
            sp.GetRequiredService<IOutletConfigStore>(),
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICredentialResolver>(),
            Directory.GetCurrentDirectory()));
        services.AddScoped<IRegistryClient, ConfiguredRegistryClient>();
        services.AddScoped<IMsBuildEvaluator, DotnetMsBuildEvaluator>();
        services.AddScoped<IProjectInspector, MsBuildProjectInspector>();
        services.AddScoped<INamespaceRewriter, RoslynNamespaceRewriter>();
        services.AddScoped<IFileSystem, PhysicalFileSystem>();
        services.AddScoped<INuGetEditor, ProjectNuGetEditor>();
        services.AddScoped<IOutletConfigStore, JsonOutletConfigStore>();

        return services;
    }
}
