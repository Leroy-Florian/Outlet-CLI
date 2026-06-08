using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Outlet.Cloud.Web.IntegrationTests;

public sealed class OutletCloudApiTests
{
    [Fact]
    public async Task Should_IssueScopedToken_ThatAccessesTheRegistry()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        var ownerId = await CreateUser(client, "owner@acme.test");
        var organizationId = await CreateOrganization(client, "acme", ownerId);
        var (_, secret) = await IssueToken(client, organizationId, ownerId);

        var registry = await Get(client, "/organizations/acme/registry.json", secret);
        registry.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await Get(client, "/me", secret);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await me.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("scopes").EnumerateArray().Select(s => s.GetString())
            .Should().Contain("org:acme:registry:read");
    }

    [Fact]
    public async Task Should_Reject_When_TokenIsInvalid()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        var response = await Get(client, "/organizations/acme/registry.json", "outlet_pat_garbage");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_RejectTokenIssue_When_UserIsNotAMember()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        var ownerId = await CreateUser(client, "owner@acme.test");
        var organizationId = await CreateOrganization(client, "acme", ownerId);
        var strangerId = await CreateUser(client, "stranger@acme.test");

        var response = await client.PostAsJsonAsync(
            $"/organizations/{organizationId}/tokens", new { userId = strangerId, name = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_RejectRevokedToken()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        var ownerId = await CreateUser(client, "owner@acme.test");
        var organizationId = await CreateOrganization(client, "acme", ownerId);
        var (tokenId, secret) = await IssueToken(client, organizationId, ownerId);

        var revoke = await client.DeleteAsync($"/tokens/{tokenId}");
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var registry = await Get(client, "/organizations/acme/registry.json", secret);
        registry.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<Guid> CreateUser(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/users", new { email, displayName = "User" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
    }

    private static async Task<Guid> CreateOrganization(HttpClient client, string slug, Guid ownerId)
    {
        var response = await client.PostAsJsonAsync("/organizations", new { slug, name = "Acme Corp", ownerUserId = ownerId });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("organizationId").GetGuid();
    }

    private static async Task<(Guid TokenId, string Secret)> IssueToken(HttpClient client, Guid organizationId, Guid userId)
    {
        var response = await client.PostAsJsonAsync($"/organizations/{organizationId}/tokens", new { userId, name = "ci" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("tokenId").GetGuid(), body.GetProperty("secret").GetString()!);
    }

    private static async Task<HttpResponseMessage> Get(HttpClient client, string path, string? bearer)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return await client.SendAsync(request);
    }
}
