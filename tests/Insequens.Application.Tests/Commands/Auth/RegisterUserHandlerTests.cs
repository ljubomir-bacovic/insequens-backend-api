using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Options;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Abstractions.Email;

namespace Insequens.Application.Tests.Commands.Auth;

public class RegisterUserHandlerTests
{
    private const string Email = "user@example.com";
    private const string Password = "Valid-Passw0rd";

    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    [Fact]
    public async Task Handle_WithNewEmail_CreatesUserAndSendsEncodedConfirmationLink()
    {
        var user = new AuthUser(Guid.NewGuid(), Email);
        _identityService.CreateUserAsync(Email, Password, Arg.Any<CancellationToken>()).Returns(user);
        _identityService.GenerateEmailConfirmationTokenAsync(user.Id, Arg.Any<CancellationToken>()).Returns("token/with+chars&=");
        EmailMessage? sent = null;
        await _emailSender.SendEmailAsync(Arg.Do<EmailMessage>(message => sent = message), Arg.Any<CancellationToken>());

        var response = await CreateHandler("https://app.example.com/").Handle(new RegisterUserCommand(Email, Password), CancellationToken.None);

        response.Should().Be(AuthResponses.RegistrationAccepted);
        var expectedLink = $"https://app.example.com/confirm-email?userId={user.Id}&token=token%2Fwith%2Bchars%26%3D";
        sent.Should().NotBeNull();
        sent!.To.Should().Be(Email);
        sent.TextBody.Should().EndWith(expectedLink);
        sent.HtmlBody.Should().Contain($"href=\"{expectedLink.Replace("&", "&amp;")}\"");
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsSameResponseWithoutCreatingOrSending()
    {
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(new AuthUser(Guid.NewGuid(), Email));

        var response = await CreateHandler().Handle(new RegisterUserCommand(Email, Password), CancellationToken.None);

        response.Should().BeSameAs(AuthResponses.RegistrationAccepted);
        await _identityService.DidNotReceive().CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIdentityRejectsUser_ReturnsSameResponseWithoutSending()
    {
        _identityService.CreateUserAsync(Email, Password, Arg.Any<CancellationToken>()).Returns((AuthUser?)null);

        var response = await CreateHandler().Handle(new RegisterUserCommand(Email, Password), CancellationToken.None);

        response.Should().BeSameAs(AuthResponses.RegistrationAccepted);
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    private RegisterUserHandler CreateHandler(string baseUrl = "https://app.example.com") => new(
        _identityService,
        _emailSender,
        Microsoft.Extensions.Options.Options.Create(new FrontendOptions { BaseUrl = baseUrl }),
        NullLogger<RegisterUserHandler>.Instance);
}
