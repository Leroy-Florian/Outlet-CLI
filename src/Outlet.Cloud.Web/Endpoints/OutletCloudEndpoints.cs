using Outlet.Cloud.Web.Authentication;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// The machine-facing registry endpoint: authenticated by a personal access token
/// (bearer), authorized by the token's scopes. Human/management endpoints live in
/// <see cref="OrganizationManagementEndpoints"/> (cookie session).
/// </summary>
public static class OutletCloudEndpoints
{
    public static void MapOutletCloud(this WebApplication app)
    {
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
