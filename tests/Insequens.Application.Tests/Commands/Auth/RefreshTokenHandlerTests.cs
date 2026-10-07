using System.Security.Claims;
using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Auth;

public class RefreshTokenHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthUser _user = new(Guid.NewGuid(), "user@example.com");

    [Fact]
    public async Task Handle_WithValidPair_RotatesTokens()
    {
        var newRefreshToken = new IssuedRefreshToken("new-refresh", DateTimeOffset.UnixEpoch);
        _tokenService.ValidateExpiredAccessTokenAsync("access").Returns(ExpiredAccessTokenResult.Valid(_user.Id));
        _identityService.ValidateRefreshTokenAsync(_user.Id, "refresh", Arg.Any<CancellationToken>()).Returns(true);
        _identityService.FindByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _tokenService.CreateAccessToken(_user.Id, Arg.Any<IEnumerable<Claim>>()).Returns(new AccessToken("new-access", DateTimeOffset.UnixEpoch));
        _tokenService.CreateRefreshToken().Returns(newRefreshToken);

        var response = await CreateHandler().Handle(new RefreshTokenCommand("access", "refresh"), CancellationToken.None);

        response.Should().Be(new AuthTokensResponse("new-access", "new-refresh"));
        await _identityService.Received(1).StoreRefreshTokenAsync(_user.Id, newRefreshToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidAccessToken_ThrowsWithoutCheckingRefreshToken()
    {
        _tokenService.ValidateExpiredAccessTokenAsync("access").Returns(ExpiredAccessTokenResult.Invalid);

        var action = () => CreateHandler().Handle(new RefreshTokenCommand("access", "refresh"), CancellationToken.None);

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        await _identityService.DidNotReceive().ValidateRefreshTokenAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidRefreshToken_ThrowsAuthenticationFailed()
    {
        _tokenService.ValidateExpiredAccessTokenAsync("access").Returns(ExpiredAccessTokenResult.Valid(_user.Id));
        _identityService.ValidateRefreshTokenAsync(_user.Id, "refresh", Arg.Any<CancellationToken>()).Returns(false);

        var action = () => CreateHandler().Handle(new RefreshTokenCommand("access", "refresh"), CancellationToken.None);

        await action.Should().ThrowAsync<AuthenticationFailedException>();
        _tokenService.DidNotReceive().CreateRefreshToken();
    }

    [Fact]
    public async Task Handle_WhenUserNoLongerExists_ThrowsAuthenticationFailed()
    {
        _tokenService.ValidateExpiredAccessTokenAsync("access").Returns(ExpiredAccessTokenResult.Valid(_user.Id));
        _identityService.ValidateRefreshTokenAsync(_user.Id, "refresh", Arg.Any<CancellationToken>()).Returns(true);

        var action = () => CreateHandler().Handle(new RefreshTokenCommand("access", "refresh"), CancellationToken.None);

        await action.Should().ThrowAsync<AuthenticationFailedException>();
    }

    private RefreshTokenHandler CreateHandler() => new(_identityService, _tokenService, NullLogger<RefreshTokenHandler>.Instance);
}
