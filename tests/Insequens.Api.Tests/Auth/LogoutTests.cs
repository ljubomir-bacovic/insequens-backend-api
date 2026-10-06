using System.Net;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Auth;

public class LogoutTests
{
    [Fact]
    public async Task Logout_InvalidatesRefreshToken()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        client.UseBearer(tokens.Token);

        var logoutResponse = await client.PostAsync("/v1/Auth/logout", content: null);
        var refreshResponse = await client.RefreshAsync(tokens.Token, tokens.RefreshToken);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await factory.FindUserAsync(Email))!.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync("/v1/Auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
