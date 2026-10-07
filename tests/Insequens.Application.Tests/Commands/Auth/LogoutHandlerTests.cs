using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Tests.Support;

namespace Insequens.Application.Tests.Commands.Auth;

public sealed class LogoutHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithSession_RevokesOnlyThatSession()
    {
        var current = await _database.SeedRefreshTokenAsync(_userId, "this-device");
        var otherDevice = await _database.SeedRefreshTokenAsync(_userId, "other-device");

        var response = await _database.SendAsync(new LogoutCommand(_userId, current.FamilyId));

        response.Should().Be(AuthResponses.LoggedOut);
        var tokens = await _database.RefreshTokensAsync(_userId);
        tokens.Single(token => token.Id == current.Id).RevokedAt.Should().Be(TestDbContextFactory.StartTime.UtcDateTime);
        tokens.Single(token => token.Id == otherDevice.Id).RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Send_WithoutSession_RevokesEverySession()
    {
        await _database.SeedRefreshTokenAsync(_userId, "this-device");
        await _database.SeedRefreshTokenAsync(_userId, "other-device");

        await _database.SendAsync(new LogoutCommand(_userId));

        (await _database.RefreshTokensAsync(_userId)).Should().OnlyContain(token => token.RevokedAt != null);
    }

    [Fact]
    public async Task Send_WithAnotherUsersSession_RevokesNothing()
    {
        var otherUsersToken = await _database.SeedRefreshTokenAsync(Guid.NewGuid(), "refresh");

        await _database.SendAsync(new LogoutCommand(_userId, otherUsersToken.FamilyId));

        (await _database.RefreshTokensAsync(otherUsersToken.UserId)).Single().RevokedAt.Should().BeNull();
    }
}
