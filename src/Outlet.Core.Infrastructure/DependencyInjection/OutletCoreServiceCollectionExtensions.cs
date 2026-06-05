using Microsoft.Extensions.DependencyInjection;
using Outlet.Core.Application.Ports;
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

        services.AddScoped<IRegistryClient, HttpRegistryClient>();
        services.AddScoped<IProjectInspector, MsBuildProjectInspector>();
        services.AddScoped<INamespaceRewriter, RoslynNamespaceRewriter>();
        services.AddScoped<IFileSystem, PhysicalFileSystem>();
        services.AddScoped<INuGetEditor, ProjectNuGetEditor>();

        return services;
    }
}
