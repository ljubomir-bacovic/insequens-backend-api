using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using Insequens.Contracts.V1.Auth;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Account;

public class ChangeEmailTests
{
    private const string NewEmail = "new@example.com";

    [Fact]
    public async Task ChangeEmail_ConfirmedThroughTheLink_SwitchesSignInToTheNewAddress()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        var tokens = await client.LoginForTokensAsync();
        client.UseBearer(tokens.Token);

        var requestResponse = await client.PostAsJsonAsync("/v1/Account/change-email", new { newEmail = NewEmail, currentPassword = Password });
        var link = ExtractLink(factory.EmailSender.Messages.Single(message => message.To == NewEmail));
        using var anonymous = factory.CreateHttpsClient();
        var confirmResponse = await anonymous.PostAsJsonAsync("/v1/Account/confirm-email-change", new
        {
            userId = QueryValue(link, "userId"),
            newEmail = QueryValue(link, "email"),
            token = QueryValue(link, "token"),
        });

        requestResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.EmailSender.Messages.Should().ContainSingle(message => message.To == Email, "the old address is told about the change");
        link.AbsolutePath.Should().Be("/confirm-email-change");
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.LoginAsync(NewEmail, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.LoginAsync(Email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.RefreshAsync(tokens.Token, tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeEmail_ToAnAddressInUse_ReturnsTheSameResponseAndSendsNothing()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        await factory.CreateUserAsync(NewEmail, Password);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var takenResponse = await client.PostAsJsonAsync("/v1/Account/change-email", new { newEmail = NewEmail, currentPassword = Password });
        var freeResponse = await client.PostAsJsonAsync("/v1/Account/change-email", new { newEmail = "free@example.com", currentPassword = Password });

        takenResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await takenResponse.Content.ReadFromJsonAsync<AuthMessageResponse>())
            .Should().Be(await freeResponse.Content.ReadFromJsonAsync<AuthMessageResponse>());
        factory.EmailSender.Messages.Should().NotContain(message => message.To == NewEmail);
    }

    [Fact]
    public async Task ConfirmEmailChange_WithInvalidToken_Returns400AndKeepsTheAddress()
    {
        await using var factory = new InsequensApiFactory();
        var user = await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Account/confirm-email-change", new
        {
            userId = user.Id.ToString(),
            newEmail = NewEmail,
            token = "forged",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.LoginAsync(Email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeEmail_WithWrongPassword_Returns400AndSendsNothing()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync(Email, Password);
        using var client = factory.CreateHttpsClient();
        client.UseBearer((await client.LoginForTokensAsync()).Token);

        var response = await client.PostAsJsonAsync("/v1/Account/change-email", new { newEmail = NewEmail, currentPassword = "Wrong-Passw0rd" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.EmailSender.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangeEmail_WithoutToken_Returns401()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Account/change-email", new { newEmail = NewEmail, currentPassword = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
