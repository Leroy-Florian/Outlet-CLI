using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Infrastructure.Persistence;

namespace Outlet.Cloud.Infrastructure.DependencyInjection;

/// <summary>Composition entry point for the Cloud context's persistence adapters.</summary>
public static class CloudInfrastructureServiceCollectionExtensions
{
    /// <summary>Registers the Cloud <see cref="CloudDbContext"/> (PostgreSQL) and its repositories.</summary>
    public static IServiceCollection AddOutletCloudInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CloudDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrganizationRepository, EfOrganizationRepository>();

        return services;
    }
}
