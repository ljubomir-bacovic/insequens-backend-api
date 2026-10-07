using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Insequens.Api.Tests.Support;
using static Insequens.Api.Tests.Support.AuthTestHelpers;

namespace Insequens.Api.Tests.Auth;

public class RegisterTests
{
    [Fact]
    public async Task Register_WithValidData_Returns202AndSendsConfirmationEmail()
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Auth/register", new { Email, Password });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var message = factory.EmailSender.Messages.Should().ContainSingle().Subject;
        message.To.Should().Be(Email);
        message.Subject.Should().Be("Please confirm your registration");

        var link = ExtractLink(message);
        link.GetLeftPart(UriPartial.Path).Should().Be($"{InsequensApiFactory.FrontendBaseUrl}/confirm-email");
        var user = await factory.FindUserAsync(Email);
        QueryValue(link, "userId").Should().Be(user!.Id.ToString());
        QueryValue(link, "token").Should().NotBeNullOrWhiteSpace();
        message.HtmlBody.Should().Contain(WebUtility.HtmlEncode(link.OriginalString));
        user.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithExistingEmail_Returns202WithIdenticalBody()
    {
        await using var factory = new InsequensApiFactory();
        await factory.CreateUserAsync("existing@example.com", Password);
        using var client = factory.CreateHttpsClient();

        var newEmailResponse = await client.PostAsJsonAsync("/v1/Auth/register", new { Email, Password });
        var existingEmailResponse = await client.PostAsJsonAsync("/v1/Auth/register", new { Email = "existing@example.com", Password });

        newEmailResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        existingEmailResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await existingEmailResponse.Content.ReadAsByteArrayAsync())
            .Should().Equal(await newEmailResponse.Content.ReadAsByteArrayAsync());
        factory.EmailSender.Messages.Should().ContainSingle().Which.To.Should().Be(Email);
    }

    [Theory]
    [InlineData("not-an-email", Password, "Email")]
    [InlineData(Email, "short1!", "Password")]
    [InlineData(Email, "no-digits-here!", "Password")]
    [InlineData(Email, "NoSymbol123", "Password")]
    public async Task Register_WithInvalidData_Returns400WithoutCreatingUser(string email, string password, string invalidField)
    {
        await using var factory = new InsequensApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/v1/Auth/register", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty(invalidField, out _).Should().BeTrue();
        factory.EmailSender.Messages.Should().BeEmpty();
        (await factory.FindUserAsync(email)).Should().BeNull();
    }
}
