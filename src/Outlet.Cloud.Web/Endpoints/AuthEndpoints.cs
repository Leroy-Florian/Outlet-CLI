using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// Interactive (human) authentication for the Outlet Cloud web UI, backed by ASP.NET
/// Core Identity with a session cookie. This is the SSO entry point the shadcn login
/// page calls; machine access uses personal access tokens instead.
/// </summary>
public static class AuthEndpoints
{
    public static void MapOutletAuth(this WebApplication app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            UserManager<OutletIdentityUser> users,
            SignInManager<OutletIdentityUser> signIn) =>
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
            if (!result.Succeeded)
                return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            // Sign the new user in immediately (sets the session cookie).
            await signIn.SignInAsync(user, isPersistent: true);
            return Results.Created($"/users/{user.Id}", new { userId = user.Id, displayName = user.DisplayName });
        });

        app.MapPost("/auth/login", async (
            LoginRequest request,
            UserManager<OutletIdentityUser> users,
            SignInManager<OutletIdentityUser> signIn) =>
        {
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user is null)
                return Results.Unauthorized();

            var result = await signIn.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: false);
            return result.Succeeded
                ? Results.Ok(new { userId = user.Id, displayName = user.DisplayName })
                : Results.Unauthorized();
        });

        app.MapGet("/auth/me", async (ClaimsPrincipal principal, UserManager<OutletIdentityUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new { userId = user.Id, email = user.Email, displayName = user.DisplayName });
        }).RequireAuthorization();

        app.MapPost("/auth/logout", async (SignInManager<OutletIdentityUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();
    }
}

/// <summary>Self-service registration with a password.</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>Email + password login.</summary>
public sealed record LoginRequest(string Email, string Password);
