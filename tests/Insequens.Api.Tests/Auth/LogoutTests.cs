using System.Net;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Auth;

public class LogoutTests
{
    [Fact]
    public async Task Logout_InvalidatesThisSessionOnly()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        using var phone = factory.CreateHttpsClient();
        using var laptop = factory.CreateHttpsClient();
        var phoneTokens = await phone.LoginForTokensAsync();
        var laptopTokens = await laptop.LoginForTokensAsync();
        phone.UseBearer(phoneTokens.Token);

        var logoutResponse = await phone.PostAsync("/v1/Auth/logout", content: null);
        var phoneRefresh = await phone.RefreshAsync(phoneTokens.Token, phoneTokens.RefreshToken);
        var laptopRefresh = await laptop.RefreshAsync(laptopTokens.Token, laptopTokens.RefreshToken);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        phoneRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        laptopRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.RefreshTokensAsync(user.Id)).Should().Contain(token => token.RevokedAt == null);
    }

    [Fact]
    public async Task LogoutAll_InvalidatesEverySession()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        using var phone = factory.CreateHttpsClient();
        using var laptop = factory.CreateHttpsClient();
        var phoneTokens = await phone.LoginForTokensAsync();
        var laptopTokens = await laptop.LoginForTokensAsync();
        phone.UseBearer(phoneTokens.Token);

        var logoutResponse = await phone.PostAsync("/v1/Auth/logout-all", content: null);
        var phoneRefresh = await phone.RefreshAsync(phoneTokens.Token, phoneTokens.RefreshToken);
        var laptopRefresh = await laptop.RefreshAsync(laptopTokens.Token, laptopTokens.RefreshToken);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        phoneRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        laptopRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await factory.RefreshTokensAsync(user.Id)).Should().OnlyContain(token => token.RevokedAt != null);
    }

    [Fact]
    public async Task Logout_WithTokenWithoutSession_InvalidatesEverySession()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        client.UseBearer(factory.CreateAccessToken(user.Id));

        var logoutResponse = await client.PostAsync("/v1/Auth/logout", content: null);
        var refreshResponse = await client.RefreshAsync(tokens.Token, tokens.RefreshToken);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/v1/Auth/logout")]
    [InlineData("/v1/Auth/logout-all")]
    public async Task Logout_WithoutToken_Returns401(string path)
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync(path, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
