using System.Security.Claims;
using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Tests.Commands.Auth;

public class LoginHandlerTests
{
    private const string Email = "user@example.com";
    private const string Password = "Valid-Passw0rd";

    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthUser _user = new(Guid.NewGuid(), Email);

    [Fact]
    public async Task Handle_WithValidCredentials_IssuesAndStoresTokens()
    {
        var refreshToken = new IssuedRefreshToken("refresh", DateTimeOffset.UnixEpoch);
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _identityService.CheckPasswordSignInAsync(_user.Id, Password, Arg.Any<CancellationToken>()).Returns(PasswordSignInStatus.Succeeded);
        _tokenService.CreateAccessToken(_user.Id, Arg.Any<IEnumerable<Claim>>()).Returns(new AccessToken("access", DateTimeOffset.UnixEpoch));
        _tokenService.CreateRefreshToken().Returns(refreshToken);

        var response = await CreateHandler().Handle(new LoginCommand(Email, Password), CancellationToken.None);

        response.Should().Be(new AuthTokensResponse("access", "refresh"));
        _tokenService.Received(1).CreateAccessToken(
            _user.Id,
            Arg.Is<IEnumerable<Claim>>(claims => claims.Single().Type == ClaimTypes.Name && claims.Single().Value == Email));
        await _identityService.Received(1).StoreRefreshTokenAsync(_user.Id, refreshToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsAuthenticationFailed()
    {
        var action = () => CreateHandler().Handle(new LoginCommand(Email, Password), CancellationToken.None);

        await action.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Theory]
    [InlineData(PasswordSignInStatus.Failed)]
    [InlineData(PasswordSignInStatus.LockedOut)]
    [InlineData(PasswordSignInStatus.NotAllowed)]
    public async Task Handle_WhenSignInDoesNotSucceed_ThrowsAuthenticationFailedWithoutIssuingTokens(PasswordSignInStatus status)
    {
        _identityService.FindByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _identityService.CheckPasswordSignInAsync(_user.Id, Password, Arg.Any<CancellationToken>()).Returns(status);

        var action = () => CreateHandler().Handle(new LoginCommand(Email, Password), CancellationToken.None);

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        _tokenService.DidNotReceive().CreateRefreshToken();
    }

    private LoginHandler CreateHandler() => new(_identityService, _tokenService, NullLogger<LoginHandler>.Instance);
}
