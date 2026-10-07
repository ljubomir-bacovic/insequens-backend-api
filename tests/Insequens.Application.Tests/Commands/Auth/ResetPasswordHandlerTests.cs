using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Insequens.Application.Abstractions.Identity;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Tests.Commands.Auth;

public sealed class ResetPasswordHandlerTests : IDisposable
{
    private const string Email = "user@example.com";
    private const string NewPassword = "N3w-Passw0rd!";

    private readonly TestDbContextFactory _database = new();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly AuthUser _user = new(Guid.NewGuid(), Email);

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithValidToken_ResetsPasswordAndRevokesEverySession()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "phone");
        await _database.SeedRefreshTokenAsync(_user.Id, "laptop");
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _identityService.ResetPasswordAsync(_user.Id, "token", NewPassword, Arg.Any<CancellationToken>()).Returns(true);

        var response = await SendAsync(new ResetPasswordCommand(Email, "token", NewPassword));

        response.Should().Be(AuthResponses.PasswordResetAccepted);
        (await _database.RefreshTokensAsync(_user.Id)).Should().OnlyContain(token => token.RevokedAt != null);
    }

    [Fact]
    public async Task Send_WithInvalidToken_ReturnsSameResponseWithoutRevoking()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "phone");
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _identityService.ResetPasswordAsync(_user.Id, "token", NewPassword, Arg.Any<CancellationToken>()).Returns(false);

        var response = await SendAsync(new ResetPasswordCommand(Email, "token", NewPassword));

        response.Should().BeSameAs(AuthResponses.PasswordResetAccepted);
        (await _database.RefreshTokensAsync(_user.Id)).Single().RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Send_WithUnknownEmail_ReturnsSameResponseWithoutResetting()
    {
        var response = await SendAsync(new ResetPasswordCommand(Email, "token", NewPassword));

        response.Should().BeSameAs(AuthResponses.PasswordResetAccepted);
        await _identityService.DidNotReceive().ResetPasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private Task<AuthMessageResponse> SendAsync(ResetPasswordCommand command) =>
        _database.SendAsync(command, services => services.AddSingleton(_identityService));
}
