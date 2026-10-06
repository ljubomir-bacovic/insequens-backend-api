using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Options;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Auth;

public class ForgotPasswordHandlerTests
{
    private const string Email = "user+tag@example.com";

    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    [Fact]
    public async Task Handle_WithExistingEmail_SendsEncodedResetLink()
    {
        var user = new AuthUser(Guid.NewGuid(), Email);
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(user);
        _identityService.GeneratePasswordResetTokenAsync(user.Id, Arg.Any<CancellationToken>()).Returns("reset/token");
        EmailMessage? sent = null;
        await _emailSender.SendEmailAsync(Arg.Do<EmailMessage>(message => sent = message), Arg.Any<CancellationToken>());

        var response = await CreateHandler().Handle(new ForgotPasswordCommand(Email), CancellationToken.None);

        response.Should().Be(AuthResponses.PasswordResetRequested);
        const string expectedLink = "https://app.example.com/reset-password?token=reset%2Ftoken&email=user%2Btag%40example.com";
        sent!.To.Should().Be(Email);
        sent.TextBody.Should().EndWith(expectedLink);
        sent.HtmlBody.Should().Contain($"href=\"{expectedLink.Replace("&", "&amp;")}\"");
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsSameResponseWithoutSending()
    {
        var response = await CreateHandler().Handle(new ForgotPasswordCommand(Email), CancellationToken.None);

        response.Should().BeSameAs(AuthResponses.PasswordResetRequested);
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    private ForgotPasswordHandler CreateHandler() => new(
        _identityService,
        _emailSender,
        Microsoft.Extensions.Options.Options.Create(new FrontendOptions { BaseUrl = "https://app.example.com" }),
        NullLogger<ForgotPasswordHandler>.Instance);
}
