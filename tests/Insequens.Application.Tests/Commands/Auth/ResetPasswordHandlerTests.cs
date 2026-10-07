using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Tests.Commands.Auth;

public class ResetPasswordHandlerTests
{
    private const string Email = "user@example.com";

    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly AuthUser _user = new(Guid.NewGuid(), Email);

    [Fact]
    public async Task Handle_WithValidToken_ResetsPasswordAndRevokesRefreshToken()
    {
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _identityService.ResetPasswordAsync(_user.Id, "token", "N3w-Passw0rd!", Arg.Any<CancellationToken>()).Returns(true);

        var response = await CreateHandler().Handle(new ResetPasswordCommand(Email, "token", "N3w-Passw0rd!"), CancellationToken.None);

        response.Should().Be(AuthResponses.PasswordResetAccepted);
        await _identityService.Received(1).RevokeRefreshTokenAsync(_user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsSameResponseWithoutRevoking()
    {
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _identityService.ResetPasswordAsync(_user.Id, "token", "N3w-Passw0rd!", Arg.Any<CancellationToken>()).Returns(false);

        var response = await CreateHandler().Handle(new ResetPasswordCommand(Email, "token", "N3w-Passw0rd!"), CancellationToken.None);

        response.Should().BeSameAs(AuthResponses.PasswordResetAccepted);
        await _identityService.DidNotReceive().RevokeRefreshTokenAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsSameResponseWithoutResetting()
    {
        var response = await CreateHandler().Handle(new ResetPasswordCommand(Email, "token", "N3w-Passw0rd!"), CancellationToken.None);

        response.Should().BeSameAs(AuthResponses.PasswordResetAccepted);
        await _identityService.DidNotReceive().ResetPasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private ResetPasswordHandler CreateHandler() => new(_identityService, NullLogger<ResetPasswordHandler>.Instance);
}
