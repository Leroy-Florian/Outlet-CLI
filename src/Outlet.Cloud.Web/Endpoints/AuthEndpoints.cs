using Microsoft.AspNetCore.Identity;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// Interactive (human) authentication for the Outlet Cloud web UI, backed by ASP.NET
/// Core Identity (password hashing/lockout). This is the SSO entry point the shadcn
/// login page calls; machine access uses personal access tokens instead.
/// </summary>
public static class AuthEndpoints
{
    public static void MapOutletAuth(this WebApplication app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, UserManager<OutletIdentityUser> users) =>
        {
            var email = request.Email.Trim();
            var user = new OutletIdentityUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                DisplayName = request.DisplayName.Trim(),
            };

            var result = await users.CreateAsync(user, request.Password);
            return result.Succeeded
                ? Results.Created($"/users/{user.Id}", new { userId = user.Id })
                : Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });
        });

        app.MapPost("/auth/login", async (LoginRequest request, UserManager<OutletIdentityUser> users) =>
        {
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user is null || !await users.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            return Results.Ok(new { userId = user.Id, displayName = user.DisplayName });
        });
    }
}

/// <summary>Self-service registration with a password.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Email + password login.</summary>
public sealed record LoginRequest(string Email, string Password);
