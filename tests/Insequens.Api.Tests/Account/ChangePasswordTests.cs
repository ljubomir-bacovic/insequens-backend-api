using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Account;

public class ChangePasswordTests
{
    private const string NewPassword = "N3w-Passw0rd!";

    [Fact]
    public async Task ChangePassword_WithCorrectPassword_Returns204AndOnlyTheNewPasswordWorks()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        client.UseBearer(tokens.Token);

        var response = await client.PostAsJsonAsync("/v1/Account/change-password", new { currentPassword = Password, newPassword = NewPassword });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.LoginAsync(Email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.LoginAsync(Email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.RefreshAsync(tokens.Token, tokens.RefreshToken)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized, "changing the password ends every session");
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400AndKeepsThePassword()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var response = await client.PostAsJsonAsync("/v1/Account/change-password", new { currentPassword = "Wrong-Passw0rd", newPassword = NewPassword });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detail").GetString().Should().Be("The current password is incorrect.");
        (await client.LoginAsync(Email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWeakNewPassword_Returns400()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var response = await client.PostAsJsonAsync("/v1/Account/change-password", new { currentPassword = Password, newPassword = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Account/change-password", new { currentPassword = Password, newPassword = NewPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
