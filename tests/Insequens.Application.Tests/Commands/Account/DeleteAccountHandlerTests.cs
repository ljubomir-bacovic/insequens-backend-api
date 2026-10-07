using FluentAssertions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Account;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V1.Account;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Account;

public sealed class DeleteAccountHandlerTests : IDisposable
{
    private const string Password = "Current-Passw0rd";

    private readonly TestDbContextFactory _database = new();
    private readonly AccountTestServices _services = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithCorrectPassword_DisablesTheAccountRevokesSessionsAndKeepsDataForTheGracePeriod()
    {
        await _database.SeedItemAsync(_userId);
        await _database.SeedRefreshTokenAsync(_userId, "phone");
        _services.PasswordCheckReturns(_userId, Password, PasswordSignInStatus.Succeeded);
        _services.IdentityService.MarkForDeletionAsync(_userId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);

        var response = await SendAsync(new DeleteAccountCommand(_userId, Password));

        response.DeletesAfter.Should().Be(TestDbContextFactory.StartTime.AddDays(30));
        await _services.IdentityService.Received(1).MarkForDeletionAsync(
            _userId, TestDbContextFactory.StartTime.UtcDateTime, Arg.Any<CancellationToken>());
        (await _database.RefreshTokensAsync(_userId)).Single().RevokedAt.Should().NotBeNull();
        (await _database.ItemsAsync(_userId)).Should().ContainSingle("tasks stay until the purge");
    }

    [Fact]
    public async Task Send_WithWrongPassword_ThrowsWithoutDisablingTheAccount()
    {
        _services.PasswordCheckReturns(_userId, Password, PasswordSignInStatus.Failed);

        var action = () => SendAsync(new DeleteAccountCommand(_userId, Password));

        await action.Should().ThrowAsync<AccountUpdateFailedException>();
        await _services.IdentityService.DidNotReceiveWithAnyArgs().MarkForDeletionAsync(default, default, default);
    }

    [Fact]
    public async Task Send_WhenTheAccountIsAlreadyGone_ThrowsNotFound()
    {
        _services.PasswordCheckReturns(_userId, Password, PasswordSignInStatus.Succeeded);

        var action = () => SendAsync(new DeleteAccountCommand(_userId, Password));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private Task<AccountDeletionResponse> SendAsync(DeleteAccountCommand command) =>
        _database.SendAsync(command, _services.Register);
}
