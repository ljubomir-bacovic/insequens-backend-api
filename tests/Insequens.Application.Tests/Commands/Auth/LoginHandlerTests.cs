using System.Security.Claims;
using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Tests.Commands.Auth;

public sealed class LoginHandlerTests : IDisposable
{
    private const string Email = "user@example.com";
    private const string Password = "Valid-Passw0rd";

    private readonly TestDbContextFactory _database = new();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthUser _user = new(Guid.NewGuid(), Email);

    public LoginHandlerTests()
    {
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _tokenService.CreateAccessToken(_user.Id, Arg.Any<IEnumerable<Claim>>()).Returns(new AccessToken("access", DateTimeOffset.UnixEpoch));
        _tokenService.CreateRefreshToken().Returns(
            new IssuedRefreshToken("refresh-1", TestDbContextFactory.StartTime.AddDays(7)),
            new IssuedRefreshToken("refresh-2", TestDbContextFactory.StartTime.AddDays(7)));
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithValidCredentials_StoresOnlyTheHashInANewSession()
    {
        await _database.SeedUserAsync(_user.Id);
        SignInReturns(PasswordSignInStatus.Succeeded);

        var response = await SendAsync(new LoginCommand(Email, Password, "  iPhone  ", "203.0.113.7"));

        response.Should().Be(new AuthTokensResponse("access", "refresh-1"));
        var stored = (await _database.RefreshTokensAsync(_user.Id)).Should().ContainSingle().Subject;
        stored.TokenHash.Should().Be(TestDbContextFactory.HashRefreshToken("refresh-1"));
        stored.ExpiresAt.Should().Be(TestDbContextFactory.StartTime.AddDays(7).UtcDateTime);
        stored.DeviceName.Should().Be("iPhone");
        stored.CreatedByIp.Should().Be("203.0.113.7");
        stored.FamilyId.Should().NotBeEmpty();
        stored.RevokedAt.Should().BeNull();
        _tokenService.Received(1).CreateAccessToken(
            _user.Id,
            Arg.Is<IEnumerable<Claim>>(claims =>
                claims.Any(claim => claim.Type == ClaimTypes.Name && claim.Value == Email)
                && claims.Any(claim => claim.Type == AuthClaimTypes.SessionId && claim.Value == stored.FamilyId.ToString())));
    }

    [Fact]
    public async Task Send_TwiceForTheSameUser_StartsTwoIndependentSessions()
    {
        await _database.SeedUserAsync(_user.Id);
        SignInReturns(PasswordSignInStatus.Succeeded);

        await SendAsync(new LoginCommand(Email, Password));
        await SendAsync(new LoginCommand(Email, Password));

        var tokens = await _database.RefreshTokensAsync(_user.Id);
        tokens.Should().HaveCount(2);
        tokens.Select(token => token.FamilyId).Should().OnlyHaveUniqueItems();
        tokens.Should().OnlyContain(token => token.RevokedAt == null);
    }

    [Fact]
    public async Task Send_WithUnknownEmail_ThrowsAuthenticationFailed()
    {
        var action = () => SendAsync(new LoginCommand("unknown@example.com", Password));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Theory]
    [InlineData(PasswordSignInStatus.Failed)]
    [InlineData(PasswordSignInStatus.LockedOut)]
    [InlineData(PasswordSignInStatus.NotAllowed)]
    public async Task Send_WhenSignInDoesNotSucceed_ThrowsAuthenticationFailedWithoutIssuingTokens(PasswordSignInStatus status)
    {
        await _database.SeedUserAsync(_user.Id);
        SignInReturns(status);

        var action = () => SendAsync(new LoginCommand(Email, Password));

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        _tokenService.DidNotReceive().CreateRefreshToken();
        (await _database.RefreshTokensAsync(_user.Id)).Should().BeEmpty();
    }

    private void SignInReturns(PasswordSignInStatus status) =>
        _identityService.CheckPasswordSignInAsync(_user.Id, Password, Arg.Any<CancellationToken>()).Returns(status);

    private Task<AuthTokensResponse> SendAsync(LoginCommand command) => _database.SendAsync(command, services =>
    {
        services.AddSingleton(_identityService);
        services.AddSingleton(_tokenService);
    });
}
