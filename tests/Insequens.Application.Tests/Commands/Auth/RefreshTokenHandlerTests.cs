using System.Security.Claims;
using FluentAssertions;
using Insequens.Application.Abstractions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Tests.Commands.Auth;

public sealed class RefreshTokenHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthUser _user = new(Guid.NewGuid(), "user@example.com");

    public RefreshTokenHandlerTests()
    {
        _tokenService.ValidateExpiredAccessTokenAsync("access").Returns(ExpiredAccessTokenResult.Valid(_user.Id));
        _identityService.FindByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _tokenService.CreateAccessToken(_user.Id, Arg.Any<IEnumerable<Claim>>()).Returns(new AccessToken("new-access", DateTimeOffset.UnixEpoch));
        _tokenService.CreateRefreshToken().Returns(
            new IssuedRefreshToken("rotated-1", TestDbContextFactory.StartTime.AddDays(7)),
            new IssuedRefreshToken("rotated-2", TestDbContextFactory.StartTime.AddDays(7)));
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithActiveToken_RotatesItWithinTheSameSession()
    {
        var original = await _database.SeedRefreshTokenAsync(_user.Id, "refresh");

        var response = await SendAsync(new RefreshTokenCommand("access", "refresh", "198.51.100.4"));

        response.Should().Be(new AuthTokensResponse("new-access", "rotated-1"));
        var tokens = await _database.RefreshTokensAsync(_user.Id);
        var retired = tokens.Single(token => token.Id == original.Id);
        var replacement = tokens.Single(token => token.Id != original.Id);
        retired.RevokedAt.Should().Be(TestDbContextFactory.StartTime.UtcDateTime);
        retired.ReplacedByTokenHash.Should().Be(replacement.TokenHash);
        replacement.TokenHash.Should().Be(TestDbContextFactory.HashRefreshToken("rotated-1"));
        replacement.FamilyId.Should().Be(original.FamilyId);
        replacement.CreatedByIp.Should().Be("198.51.100.4");
        replacement.IsActive(TestDbContextFactory.StartTime.UtcDateTime).Should().BeTrue();
        _tokenService.Received(1).CreateAccessToken(
            _user.Id,
            Arg.Is<IEnumerable<Claim>>(claims =>
                claims.Any(claim => claim.Type == AuthClaimTypes.SessionId && claim.Value == original.FamilyId.ToString())));
    }

    [Fact]
    public async Task Send_WithTokenThatWasAlreadyRotated_RevokesTheWholeSession()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "refresh");
        await SendAsync(new RefreshTokenCommand("access", "refresh"));

        var reuse = () => SendAsync(new RefreshTokenCommand("access", "refresh"));

        await reuse.Should().ThrowAsync<AuthenticationFailedException>();
        (await _database.RefreshTokensAsync(_user.Id)).Should().HaveCount(2).And.OnlyContain(token => token.RevokedAt != null);
        var rotatedTokenStillWorks = () => SendAsync(new RefreshTokenCommand("access", "rotated-1"));
        await rotatedTokenStillWorks.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Send_WithReusedToken_LeavesOtherSessionsActive()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "refresh");
        var otherDevice = await _database.SeedRefreshTokenAsync(_user.Id, "other-device");
        await SendAsync(new RefreshTokenCommand("access", "refresh"));

        var reuse = () => SendAsync(new RefreshTokenCommand("access", "refresh"));

        await reuse.Should().ThrowAsync<AuthenticationFailedException>();
        (await _database.RefreshTokensAsync(_user.Id)).Single(token => token.Id == otherDevice.Id).RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Send_WithExpiredToken_ThrowsWithoutRotating()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "refresh", lifetime: TimeSpan.FromDays(7));
        _database.Clock.Advance(TimeSpan.FromDays(7));

        var action = () => SendAsync(new RefreshTokenCommand("access", "refresh"));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        _tokenService.DidNotReceive().CreateRefreshToken();
    }

    [Fact]
    public async Task Send_WithUnknownToken_ThrowsAuthenticationFailed()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "refresh");

        var action = () => SendAsync(new RefreshTokenCommand("access", "guessed"));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        _tokenService.DidNotReceive().CreateRefreshToken();
    }

    [Fact]
    public async Task Send_WithAnotherUsersToken_ThrowsWithoutRevokingIt()
    {
        var otherUsersToken = await _database.SeedRefreshTokenAsync(Guid.NewGuid(), "refresh");

        var action = () => SendAsync(new RefreshTokenCommand("access", "refresh"));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        (await _database.RefreshTokensAsync(otherUsersToken.UserId)).Single().RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Send_WithInvalidAccessToken_ThrowsWithoutLookingUpTheRefreshToken()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "refresh");
        _tokenService.ValidateExpiredAccessTokenAsync("forged").Returns(ExpiredAccessTokenResult.Invalid);

        var action = () => SendAsync(new RefreshTokenCommand("forged", "refresh"));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        (await _database.RefreshTokensAsync(_user.Id)).Single().RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Send_WhenUserNoLongerExists_ThrowsAuthenticationFailed()
    {
        await _database.SeedRefreshTokenAsync(_user.Id, "refresh");
        _identityService.FindByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((AuthUser?)null);

        var action = () => SendAsync(new RefreshTokenCommand("access", "refresh"));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Send_WhenAConcurrentRefreshRotatedTheTokenFirst_ThrowsAndKeepsTheWinner()
    {
        var original = await _database.SeedRefreshTokenAsync(_user.Id, "refresh");
        var concurrentRefresh = new RotateBeforeSaveInterceptor(_database, original.Id);

        var action = () => SendAsync(
            new RefreshTokenCommand("access", "refresh"),
            services => services.Replace(ServiceDescriptor.Scoped<IApplicationDbContext>(_ => _database.CreateContext(concurrentRefresh))));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        var tokens = await _database.RefreshTokensAsync(_user.Id);
        tokens.Should().ContainSingle(token => token.Id == original.Id)
            .Which.ReplacedByTokenHash.Should().Be("concurrent-winner-hash-000000000000000000000");
    }

    private Task<AuthTokensResponse> SendAsync(RefreshTokenCommand command, Action<IServiceCollection>? configure = null) =>
        _database.SendAsync(command, services =>
        {
            services.AddSingleton(_identityService);
            services.AddSingleton(_tokenService);
            configure?.Invoke(services);
        });

    /// <summary>Rotates the token from a second context just before the handler saves, as a parallel request would.</summary>
    private sealed class RotateBeforeSaveInterceptor(TestDbContextFactory database, Guid tokenId) : SaveChangesInterceptor
    {
        private bool _rotated;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_rotated)
            {
                _rotated = true;
                await using var other = database.CreateContext();
                var token = await other.RefreshTokens.SingleAsync(candidate => candidate.Id == tokenId, cancellationToken);
                other.RefreshTokens.Add(token.Rotate(
                    "concurrent-winner-hash-000000000000000000000",
                    token.ExpiresAt,
                    createdByIp: null,
                    database.Clock.GetUtcNow().UtcDateTime));
                await other.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
    }
}
