using Outlet.Cloud.Application.Organizations;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Web.Authentication;
using Outlet.Cloud.Web.Composition;
using Outlet.Identity.Application.AccessTokens;
using Outlet.Identity.Application.Users;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>The Outlet Cloud management API: users, organizations, memberships, tokens,
/// plus a bearer-gated registry endpoint that enforces the token's scopes.</summary>
public static class OutletCloudEndpoints
{
    public static void MapOutletCloud(this WebApplication app)
    {
        app.MapPost("/users", async (RegisterUserRequest request, RegisterUserUseCase useCase, CancellationToken ct) =>
            (await useCase.HandleAsync(new RegisterUserCommand(Guid.NewGuid(), request.Email, request.DisplayName), ct))
                .ToHttp(id => Results.Created($"/users/{id}", new { userId = id })));

        app.MapPost("/organizations", async (CreateOrganizationRequest request, CreateOrganizationUseCase useCase, CancellationToken ct) =>
            (await useCase.HandleAsync(new CreateOrganizationCommand(Guid.NewGuid(), request.Slug, request.Name, request.OwnerUserId), ct))
                .ToHttp(id => Results.Created($"/organizations/{id}", new { organizationId = id })));

        app.MapPost("/organizations/{organizationId:guid}/members", async (Guid organizationId, AddMemberRequest request, AddMemberUseCase useCase, CancellationToken ct) =>
            (await useCase.HandleAsync(new AddMemberCommand(organizationId, request.UserId, request.Role), ct)).ToHttp());

        app.MapPut("/organizations/{organizationId:guid}/members/{userId:guid}", async (Guid organizationId, Guid userId, ChangeRoleRequest request, ChangeMemberRoleUseCase useCase, CancellationToken ct) =>
            (await useCase.HandleAsync(new ChangeMemberRoleCommand(organizationId, userId, request.Role), ct)).ToHttp());

        app.MapDelete("/organizations/{organizationId:guid}/members/{userId:guid}", async (Guid organizationId, Guid userId, RemoveMemberUseCase useCase, CancellationToken ct) =>
            (await useCase.HandleAsync(new RemoveMemberCommand(organizationId, userId), ct)).ToHttp());

        app.MapPost("/organizations/{organizationId:guid}/tokens", async (Guid organizationId, IssueTokenRequest request, OrganizationTokenIssuer issuer, CancellationToken ct) =>
            (await issuer.IssueAsync(organizationId, request.UserId, request.Name, request.ExpiresAtUtc, ct))
                .ToHttp(token => Results.Created($"/tokens/{token.TokenId}", token)));

        app.MapDelete("/tokens/{tokenId:guid}", async (Guid tokenId, RevokePersonalAccessTokenUseCase useCase, CancellationToken ct) =>
            (await useCase.HandleAsync(new RevokePersonalAccessTokenCommand(tokenId), ct)).ToHttp());

        app.MapGet("/me", async (HttpRequest http, PersonalAccessTokenAuthenticator authenticator, CancellationToken ct) =>
        {
            var token = await authenticator.AuthenticateAsync(http.Headers.Authorization, ct);
            return token is null ? Results.Unauthorized() : Results.Ok(token);
        });

        app.MapGet("/organizations/{slug}/registry.json", async (string slug, HttpRequest http, PersonalAccessTokenAuthenticator authenticator, CancellationToken ct) =>
        {
            var token = await authenticator.AuthenticateAsync(http.Headers.Authorization, ct);
            if (token is null)
                return Results.Unauthorized();

            if (!token.Scopes.Contains($"org:{slug}:registry:read"))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            return Results.Ok(new { items = Array.Empty<object>() });
        });
    }
}

/// <summary>Register a user.</summary>
public sealed record RegisterUserRequest(string Email, string DisplayName);

/// <summary>Create an organization with a first owner.</summary>
public sealed record CreateOrganizationRequest(string Slug, string Name, Guid OwnerUserId);

/// <summary>Add a member with a role.</summary>
public sealed record AddMemberRequest(Guid UserId, OrganizationRole Role);

/// <summary>Change a member's role.</summary>
public sealed record ChangeRoleRequest(OrganizationRole Role);

/// <summary>Issue a scoped personal access token for an organization member.</summary>
public sealed record IssueTokenRequest(Guid UserId, string Name, DateTime? ExpiresAtUtc);
