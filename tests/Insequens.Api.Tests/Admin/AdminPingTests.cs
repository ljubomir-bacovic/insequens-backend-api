using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Application.Authorization;
using Insequens.Contracts.V1.Admin;
using Microsoft.IdentityModel.JsonWebTokens;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Admin;

public class AdminPingTests
{
    private const string Path = "/v1/admin/ping";

    [Fact]
    public async Task Ping_AsAdmin_Returns200()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        await factory.AddToRoleAsync(user, Roles.Admin);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var response = await client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdminPingResponse>();
        body!.Status.Should().Be("ok");
        body.ServerTime.Should().Be(factory.Clock.GetUtcNow());
    }

    [Fact]
    public async Task Ping_AsNormalUser_Returns403()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var response = await client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ping_AsSupport_Returns403()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        await factory.AddToRoleAsync(user, Roles.Support);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var response = await client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ping_AfterAdminRoleWasRemoved_Returns403BeforeTheTokenExpires()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        await factory.AddToRoleAsync(user, Roles.Admin);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);
        await factory.RemoveFromRoleAsync(user, Roles.Admin);

        var response = await client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the Application policy checks the user store, not only the token");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Ping_WithoutToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(Path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AsAdmin_PutsTheRoleInTheAccessToken()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        await factory.AddToRoleAsync(user, Roles.Admin);
        using var client = factory.CreateHttpsClient();

        var tokens = await client.LoginForTokensAsync();

        new JsonWebToken(tokens.Token).Claims.Should().ContainSingle(claim => claim.Type == "role")
            .Which.Value.Should().Be(Roles.Admin);
    }
}
