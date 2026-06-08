using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Identity.Application.Ports;
using Outlet.Identity.Infrastructure.Persistence;
using Outlet.Identity.Infrastructure.Security;

namespace Outlet.Identity.Infrastructure.DependencyInjection;

/// <summary>Composition entry point for the Identity context's membership + persistence adapters.</summary>
public static class IdentityInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Identity <see cref="IdentityDataContext"/> (PostgreSQL), ASP.NET Core
    /// Identity membership over it, the repositories and the token secret factory.
    /// </summary>
    public static IServiceCollection AddOutletIdentityInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentityDataContext>(options => options.UseNpgsql(connectionString));

        services.AddIdentityCore<OutletIdentityUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDataContext>();

        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IPersonalAccessTokenRepository, EfPersonalAccessTokenRepository>();
        services.AddSingleton<ITokenSecretFactory, Sha256TokenSecretFactory>();

        return services;
    }
}
