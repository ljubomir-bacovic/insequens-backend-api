using FluentAssertions;
using Insequens.Application.Commands.Account;
using Insequens.Application.Tests.Support;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Account;

public sealed class PurgeDeletedAccountsHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly AccountTestServices _services = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_DeletesEveryDueAccountWithItsTasksAndSessionsOnly()
    {
        var deletedUser = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        await _database.SeedItemAsync(deletedUser);
        await _database.SeedRefreshTokenAsync(deletedUser, "phone");
        await _database.SeedItemAsync(otherUser);
        await _database.SeedRefreshTokenAsync(otherUser, "other");
        var cutoff = TestDbContextFactory.StartTime.AddDays(-30).UtcDateTime;
        _services.IdentityService.FindAccountsDueForPurgeAsync(cutoff, Arg.Any<CancellationToken>()).Returns([deletedUser]);

        var purged = await _database.SendAsync(new PurgeDeletedAccountsCommand(), _services.Register);

        purged.Should().Be(1);
        await _services.IdentityService.Received(1).DeleteUserAsync(deletedUser, Arg.Any<CancellationToken>());
        (await _database.ItemsAsync(deletedUser)).Should().BeEmpty();
        (await _database.RefreshTokensAsync(deletedUser)).Should().BeEmpty();
        (await _database.ItemsAsync(otherUser)).Should().ContainSingle();
        (await _database.RefreshTokensAsync(otherUser)).Should().ContainSingle();
    }

    [Fact]
    public async Task Send_WithNothingDue_DeletesNothing()
    {
        var purged = await _database.SendAsync(new PurgeDeletedAccountsCommand(), _services.Register);

        purged.Should().Be(0);
        await _services.IdentityService.DidNotReceiveWithAnyArgs().DeleteUserAsync(default, default);
    }
}
