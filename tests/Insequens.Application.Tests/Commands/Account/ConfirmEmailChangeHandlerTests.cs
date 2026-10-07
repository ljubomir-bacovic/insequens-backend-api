using FluentAssertions;
using Insequens.Application.Commands.Account;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V1.Auth;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Account;

public sealed class ConfirmEmailChangeHandlerTests : IDisposable
{
    private const string NewEmail = "new@example.com";

    private readonly TestDbContextFactory _database = new();
    private readonly AccountTestServices _services = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithValidToken_ChangesTheEmailAndRevokesEverySession()
    {
        await _database.SeedRefreshTokenAsync(_userId, "phone");
        _services.IdentityService.ChangeEmailAsync(_userId, NewEmail, "token", Arg.Any<CancellationToken>()).Returns(true);

        var response = await SendAsync(new ConfirmEmailChangeCommand(_userId.ToString(), NewEmail, "token"));

        response.Should().BeSameAs(AccountResponses.EmailChanged);
        (await _database.RefreshTokensAsync(_userId)).Single().RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Send_WithInvalidToken_ThrowsAndKeepsSessions()
    {
        await _database.SeedRefreshTokenAsync(_userId, "phone");

        var action = () => SendAsync(new ConfirmEmailChangeCommand(_userId.ToString(), NewEmail, "token"));

        await action.Should().ThrowAsync<AccountUpdateFailedException>()
            .WithMessage("The email change link is invalid or has expired.");
        (await _database.RefreshTokensAsync(_userId)).Single().RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Send_WithUserIdThatIsNotAGuid_ThrowsWithoutAskingTheStore()
    {
        var action = () => SendAsync(new ConfirmEmailChangeCommand("not-a-guid", NewEmail, "token"));

        await action.Should().ThrowAsync<AccountUpdateFailedException>();
        await _services.IdentityService.DidNotReceiveWithAnyArgs().ChangeEmailAsync(default, default!, default!, default);
    }

    private Task<AuthMessageResponse> SendAsync(ConfirmEmailChangeCommand command) =>
        _database.SendAsync(command, _services.Register);
}
