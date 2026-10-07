using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Auth;

public class PasswordResetTests
{
    private const string NewPassword = "N3w-Passw0rd!";

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_Returns202_NoEmailSent()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.EmailSender.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ForgotPassword_ForExistingEmail_SendsResetEmailWithIdenticalResponseBody()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();

        var existingResponse = await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email });
        var unknownResponse = await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email = "nobody@example.com" });

        existingResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await existingResponse.Content.ReadAsByteArrayAsync())
            .Should().Equal(await unknownResponse.Content.ReadAsByteArrayAsync());
        var message = factory.EmailSender.Messages.Should().ContainSingle().Subject;
        message.To.Should().Be(Email);
        message.Subject.Should().Be("Password Reset");
        var link = ExtractLink(message);
        link.GetLeftPart(UriPartial.Path).Should().Be($"{InsequensApiFactory.FrontendBaseUrl}/reset-password");
        QueryValue(link, "email").Should().Be(Email);
        message.HtmlBody.Should().Contain(WebUtility.HtmlEncode(link.OriginalString));
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_AllowsLoginWithNewPassword()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokensBeforeReset = await client.LoginForTokensAsync();
        await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email });
        var token = QueryValue(ExtractLink(factory.EmailSender.Messages.Single()), "token");

        var resetResponse = await client.PostAsJsonAsync("/v1/Auth/reset-password", new { Email, Token = token, NewPassword });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await client.LoginAsync(Email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.LoginAsync(Email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.RefreshAsync(tokensBeforeReset.Token, tokensBeforeReset.RefreshToken))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_ForUnknownEmailOrInvalidToken_Returns202WithIdenticalBody()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        await client.PostAsJsonAsync("/v1/Auth/forgot-password", new { Email });
        var validToken = QueryValue(ExtractLink(factory.EmailSender.Messages.Single()), "token");

        var unknownEmailResponse = await client.PostAsJsonAsync(
            "/v1/Auth/reset-password",
            new { Email = "nobody@example.com", Token = validToken, NewPassword });
        var invalidTokenResponse = await client.PostAsJsonAsync(
            "/v1/Auth/reset-password",
            new { Email, Token = "invalid-token", NewPassword });
        var validResponse = await client.PostAsJsonAsync(
            "/v1/Auth/reset-password",
            new { Email, Token = validToken, NewPassword });

        unknownEmailResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        invalidTokenResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var expectedBody = await validResponse.Content.ReadAsByteArrayAsync();
        (await unknownEmailResponse.Content.ReadAsByteArrayAsync()).Should().Equal(expectedBody);
        (await invalidTokenResponse.Content.ReadAsByteArrayAsync()).Should().Equal(expectedBody);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_Returns400()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Auth/reset-password", new { Email, Token = "token", NewPassword = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
