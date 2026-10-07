using FluentAssertions;
using Insequens.Application.Commands.Auth;
using NSubstitute;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Tests.Commands.Auth;

public class LogoutHandlerTests
{
    [Fact]
    public async Task Handle_WithUser_RevokesRefreshToken()
    {
        var identityService = Substitute.For<IIdentityService>();
        var userId = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();

        var response = await new LogoutHandler(identityService).Handle(new LogoutCommand(userId), cancellationTokenSource.Token);

        response.Should().Be(AuthResponses.LoggedOut);
        await identityService.Received(1).RevokeRefreshTokenAsync(userId, cancellationTokenSource.Token);
    }
}
