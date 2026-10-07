using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Microsoft.IdentityModel.JsonWebTokens;
using Insequens.Contracts.V1.Auth;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Auth;

public class LoginTests
{
    [Fact]
    public async Task Login_BeforeConfirmation_Returns401Generic()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password, emailConfirmed: false);
        await factory.CreateUserAsync("confirmed@example.com", Password);
        using var client = factory.CreateHttpsClient();

        var unconfirmedResponse = await client.LoginAsync();
        var wrongPasswordResponse = await client.LoginAsync("confirmed@example.com", "Wrong-Passw0rd");

        unconfirmedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertGenericUnauthorizedAsync(unconfirmedResponse);
        (await unconfirmedResponse.ReadProblemWithoutTraceIdAsync())
            .Should().Be(await wrongPasswordResponse.ReadProblemWithoutTraceIdAsync());
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401Generic()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();

        var unknownEmailResponse = await client.LoginAsync("nobody@example.com", Password);
        var wrongPasswordResponse = await client.LoginAsync(Email, "Wrong-Passw0rd");

        unknownEmailResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await unknownEmailResponse.ReadProblemWithoutTraceIdAsync())
            .Should().Be(await wrongPasswordResponse.ReadProblemWithoutTraceIdAsync());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Generic_AndIncrementsAccessFailedCount()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var response = await client.LoginAsync(Email, "Wrong-Passw0rd");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await AssertGenericUnauthorizedAsync(response);
        }

        var user = await factory.FindUserAsync(Email);
        user!.AccessFailedCount.Should().Be(3);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task Login_AfterMaxFailures_IsLockedOut()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var failures = new List<string>();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            failures.Add(await (await client.LoginAsync(Email, "Wrong-Passw0rd")).ReadProblemWithoutTraceIdAsync());
        }

        // The sixth attempt uses the correct password and is still refused, with the same body.
        var lockedOutResponse = await client.LoginAsync();

        lockedOutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await lockedOutResponse.ReadProblemWithoutTraceIdAsync()).Should().Be(failures[0]);
        failures.Should().AllBe(failures[0]);
        var user = await factory.FindUserAsync(Email);
        user!.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessAndRefreshTokens_AccessTokenExpiresIn15Minutes()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();

        var response = await client.LoginAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        tokens!.RefreshToken.Should().NotBeNullOrWhiteSpace();
        var accessToken = new JsonWebTokenHandler().ReadJsonWebToken(tokens.Token);
        (accessToken.ValidTo - accessToken.IssuedAt).Should().Be(TimeSpan.FromMinutes(15));
        accessToken.Issuer.Should().Be(InsequensApiFactory.JwtIssuer);
        accessToken.Audiences.Should().Equal(InsequensApiFactory.JwtAudience);
        accessToken.GetClaim(JwtRegisteredClaimNames.NameId).Value.Should().Be(user.Id.ToString());
        accessToken.GetClaim(JwtRegisteredClaimNames.UniqueName).Value.Should().Be(Email);
        accessToken.Alg.Should().Be("HS256");
    }

    [Fact]
    public async Task Login_WithValidCredentials_StoresOnlyAHashOfTheRefreshToken()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();

        var tokens = await client.LoginForTokensAsync();

        var user = await factory.FindUserAsync(Email);
        var stored = (await factory.RefreshTokensAsync(user!.Id)).Should().ContainSingle().Subject;
        stored.TokenHash.Should().Be(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken))));
        stored.TokenHash.Should().NotBe(tokens.RefreshToken);
        stored.ExpiresAt.Should().Be(factory.Clock.GetUtcNow().AddDays(7).UtcDateTime);
    }

    [Theory]
    [InlineData("", Password, "Email")]
    [InlineData("not-an-email", Password, "Email")]
    [InlineData(Email, "", "Password")]
    public async Task Login_WithInvalidRequest_Returns400(string email, string password, string invalidField)
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.LoginAsync(email, password);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty(invalidField, out _).Should().BeTrue();
    }

    private static async Task AssertGenericUnauthorizedAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().Should().Be(401);
        body.RootElement.GetProperty("title").GetString().Should().Be("Authentication failed.");
        body.RootElement.TryGetProperty("detail", out _).Should().BeFalse();
    }
}
