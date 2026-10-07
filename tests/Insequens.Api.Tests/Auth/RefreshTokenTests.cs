using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Insequens.Contracts.V1.Auth;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Auth;

public class RefreshTokenTests
{
    [Fact]
    public async Task Refresh_WithValidPair_RotatesRefreshToken_OldOneRejected()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var original = await client.LoginForTokensAsync();

        var refreshResponse = await client.RefreshAsync(original.Token, original.RefreshToken);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensResponse>();
        var reuseResponse = await client.RefreshAsync(original.Token, original.RefreshToken);
        var secondRefreshResponse = await client.RefreshAsync(rotated!.Token, rotated.RefreshToken);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        rotated.RefreshToken.Should().NotBe(original.RefreshToken);
        rotated.Token.Should().NotBe(original.Token);
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithExpiredAccessToken_Succeeds()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        factory.Clock.Advance(TimeSpan.FromHours(1));

        var response = await client.RefreshAsync(tokens.Token, tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithTamperedAccessToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        var forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = InsequensApiFactory.JwtIssuer,
            Audience = InsequensApiFactory.JwtAudience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString())]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("an-attacker-signing-key-of-32-characters")),
                SecurityAlgorithms.HmacSha256),
        });

        var response = await client.RefreshAsync(forged, tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertAuthenticationFailedAsync(response);
    }

    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("a.b.c")]
    [InlineData("eyJhbGciOiJub25lIn0.eyJuYW1laWQiOiJ4In0.")]
    public async Task Refresh_WithMalformedAccessToken_Returns401ProblemDetails(string malformedToken)
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();

        var response = await client.RefreshAsync(malformedToken, tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertAuthenticationFailedAsync(response);
    }

    [Fact]
    public async Task Refresh_WithExpiredRefreshToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        factory.Clock.Advance(TimeSpan.FromDays(7));

        var response = await client.RefreshAsync(tokens.Token, tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertAuthenticationFailedAsync(response);
    }

    [Fact]
    public async Task Refresh_WithAnotherUsersRefreshToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        await factory.CreateUserAsync("other@example.com", Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        var otherTokens = await client.LoginForTokensAsync("other@example.com");

        var response = await client.RefreshAsync(tokens.Token, otherTokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithEmptyTokens_Returns400()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.RefreshAsync(string.Empty, string.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task AssertAuthenticationFailedAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("title").GetString().Should().Be("Authentication failed.");
    }
}
