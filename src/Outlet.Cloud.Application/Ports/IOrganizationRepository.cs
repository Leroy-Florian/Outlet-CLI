using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="Organization"/> aggregates.</summary>
public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithSlugAsync(OrganizationSlug slug, CancellationToken cancellationToken = default);

    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);

    Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default);
}
