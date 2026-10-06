using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Auth;

public class ConfirmEmailTests
{
    [Fact]
    public async Task ConfirmEmail_WithValidToken_EnablesLogin()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();
        await client.PostAsJsonAsync("/v1/Auth/register", new { Email, Password });
        var link = ExtractLink(factory.EmailSender.Messages.Single());

        var loginBeforeConfirmation = await client.LoginAsync();
        var confirmResponse = await client.GetAsync(
            $"/v1/Auth/confirm-email?userId={Uri.EscapeDataString(QueryValue(link, "userId"))}&token={Uri.EscapeDataString(QueryValue(link, "token"))}");
        var loginAfterConfirmation = await client.LoginAsync();

        loginBeforeConfirmation.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginAfterConfirmation.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task ConfirmEmail_WithUnknownUser_Returns400(string userId)
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/v1/Auth/confirm-email?userId={userId}&token=some-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("title").GetString().Should().Be("Email confirmation failed.");
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_Returns400AndLeavesEmailUnconfirmed()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password, emailConfirmed: false);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/v1/Auth/confirm-email?userId={user.Id}&token=invalid-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.FindUserAsync(Email))!.EmailConfirmed.Should().BeFalse();
    }
}
