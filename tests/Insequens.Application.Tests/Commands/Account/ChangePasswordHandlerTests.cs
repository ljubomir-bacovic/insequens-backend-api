using FluentAssertions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Account;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Account;

public sealed class ChangePasswordHandlerTests : IDisposable
{
    private const string Current = "Current-Passw0rd";
    private const string New = "N3w-Passw0rd!";

    private readonly TestDbContextFactory _database = new();
    private readonly AccountTestServices _services = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithCorrectPassword_ChangesItAndRevokesEverySession()
    {
        await _database.SeedRefreshTokenAsync(_userId, "phone");
        await _database.SeedRefreshTokenAsync(_userId, "laptop");
        _services.PasswordCheckReturns(_userId, Current, PasswordSignInStatus.Succeeded);
        _services.IdentityService.ChangePasswordAsync(_userId, Current, New, Arg.Any<CancellationToken>()).Returns(true);

        await SendAsync(new ChangePasswordCommand(_userId, Current, New));

        await _services.IdentityService.Received(1).ChangePasswordAsync(_userId, Current, New, Arg.Any<CancellationToken>());
        (await _database.RefreshTokensAsync(_userId)).Should().OnlyContain(token => token.RevokedAt != null);
    }

    [Theory]
    [InlineData(PasswordSignInStatus.Failed, "The current password is incorrect.")]
    [InlineData(PasswordSignInStatus.NotAllowed, "The current password is incorrect.")]
    [InlineData(PasswordSignInStatus.LockedOut, "Too many failed attempts. Try again later.")]
    public async Task Send_WhenCurrentPasswordIsNotAccepted_ThrowsWithoutChangingAnything(PasswordSignInStatus status, string reason)
    {
        await _database.SeedRefreshTokenAsync(_userId, "phone");
        _services.PasswordCheckReturns(_userId, Current, status);

        var action = () => SendAsync(new ChangePasswordCommand(_userId, Current, New));

        await action.Should().ThrowAsync<AccountUpdateFailedException>().WithMessage(reason);
        await _services.IdentityService.DidNotReceiveWithAnyArgs().ChangePasswordAsync(default, default!, default!, default);
        (await _database.RefreshTokensAsync(_userId)).Single().RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Send_WhenTheStoreRejectsTheNewPassword_ThrowsAndKeepsSessions()
    {
        await _database.SeedRefreshTokenAsync(_userId, "phone");
        _services.PasswordCheckReturns(_userId, Current, PasswordSignInStatus.Succeeded);

        var action = () => SendAsync(new ChangePasswordCommand(_userId, Current, New));

        await action.Should().ThrowAsync<AccountUpdateFailedException>();
        (await _database.RefreshTokensAsync(_userId)).Single().RevokedAt.Should().BeNull();
    }

    private Task SendAsync(ChangePasswordCommand command) => _database.SendAsync(command, _services.Register);
}
