using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Tests.Support;

namespace Insequens.Application.Tests.Commands.Auth;

public sealed class LogoutEverywhereHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_RevokesEverySessionOfTheUserOnly()
    {
        var userId = Guid.NewGuid();
        await _database.SeedRefreshTokenAsync(userId, "phone");
        await _database.SeedRefreshTokenAsync(userId, "laptop");
        var otherUsersToken = await _database.SeedRefreshTokenAsync(Guid.NewGuid(), "other-user");

        var response = await _database.SendAsync(new LogoutEverywhereCommand(userId));

        response.Should().Be(AuthResponses.LoggedOutEverywhere);
        (await _database.RefreshTokensAsync(userId)).Should().HaveCount(2).And.OnlyContain(token => token.RevokedAt != null);
        (await _database.RefreshTokensAsync(otherUsersToken.UserId)).Single().RevokedAt.Should().BeNull();
    }
}
