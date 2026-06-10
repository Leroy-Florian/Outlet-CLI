using Microsoft.Extensions.DependencyInjection;
using Outlet.Core.Application.Ports;
using Outlet.Core.Infrastructure.Cli;
using Outlet.Core.Infrastructure.Configuration;
using Outlet.Core.Infrastructure.Io;
using Outlet.Core.Infrastructure.NuGet;
using Outlet.Core.Infrastructure.Projects;
using Outlet.Core.Infrastructure.Registry;
using Outlet.Core.Infrastructure.Rewriting;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Infrastructure.DependencyInjection;

/// <summary>
/// Explicit DI registration of the secondary adapters — no assembly scanning,
/// per the Outlet design principles.
/// </summary>
public static class OutletCoreServiceCollectionExtensions
{
    /// <summary>NuGet id of the published CLI tool — the self-update target.</summary>
    public const string CliPackageId = "Outlet.Cli";

    /// <summary>NuGet "flat container" feed used to discover the latest CLI version.</summary>
    public const string NuGetFlatContainerBaseUri = "https://api.nuget.org/v3-flatcontainer/";

    public static IServiceCollection AddOutletCoreInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<HttpClient>(_ => new HttpClient());

        // Self-update (Level 1) and the throttled background version check (Level 2).
        services.AddSingleton<ICurrentDateTimeProvider, UtcDateTimeProvider>();
        services.AddScoped<ICliReleaseClient>(sp => new HttpNuGetCliReleaseClient(
            sp.GetRequiredService<HttpClient>(),
            new Uri(NuGetFlatContainerBaseUri),
            CliPackageId));
        services.AddScoped<ICliUpdater>(_ => new DotnetToolCliUpdater(CliPackageId));
        services.AddScoped<ICliUpdateStateStore>(_ => new JsonFileCliUpdateStateStore());

        // Registry sources are read from the project's outlet.json (per working
        // directory), then the client fans out across them.
        services.AddScoped<IRegistrySourceProvider>(sp => new ConfiguredRegistrySourceProvider(
            sp.GetRequiredService<IOutletConfigStore>(),
            sp.GetRequiredService<HttpClient>(),
            Directory.GetCurrentDirectory()));
        services.AddScoped<IRegistryClient, ConfiguredRegistryClient>();
        services.AddScoped<IRegistryPublisher, RegistryCatalogPublisher>();
        services.AddScoped<IMsBuildEvaluator, DotnetMsBuildEvaluator>();
        services.AddScoped<IProjectInspector, MsBuildProjectInspector>();
        services.AddScoped<IPackageRestorer, DotnetPackageRestorer>();
        services.AddScoped<INamespaceRewriter, RoslynNamespaceRewriter>();
        services.AddScoped<IFileSystem, PhysicalFileSystem>();
        services.AddScoped<INuGetEditor, ProjectNuGetEditor>();
        services.AddScoped<IOutletConfigStore, JsonOutletConfigStore>();

        return services;
    }
}
