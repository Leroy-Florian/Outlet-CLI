using Microsoft.AspNetCore.Identity;

namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>
/// ASP.NET Core Identity membership entity for the <c>User</c> aggregate: it owns the
/// credential concerns (password hash, lockout, 2FA, confirmation) while the domain
/// keeps only identity language. Keyed by the same GUID as the domain <c>UserId</c>.
/// </summary>
public sealed class OutletIdentityUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
