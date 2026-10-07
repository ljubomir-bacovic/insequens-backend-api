using FluentAssertions;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using NSubstitute;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Tests.Commands.Auth;

public class ConfirmEmailHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();

    [Fact]
    public async Task Handle_WithValidToken_ReturnsConfirmedResponse()
    {
        var userId = Guid.NewGuid();
        _identityService.ConfirmEmailAsync(userId, "token", Arg.Any<CancellationToken>()).Returns(true);

        var response = await new ConfirmEmailHandler(_identityService)
            .Handle(new ConfirmEmailCommand(userId.ToString(), "token"), CancellationToken.None);

        response.Should().Be(AuthResponses.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ThrowsEmailConfirmationFailedException()
    {
        var userId = Guid.NewGuid();
        _identityService.ConfirmEmailAsync(userId, "token", Arg.Any<CancellationToken>()).Returns(false);

        var action = () => new ConfirmEmailHandler(_identityService)
            .Handle(new ConfirmEmailCommand(userId.ToString(), "token"), CancellationToken.None);

        await action.Should().ThrowAsync<EmailConfirmationFailedException>();
    }

    [Fact]
    public async Task Handle_WithNonGuidUserId_ThrowsWithoutCallingIdentity()
    {
        var action = () => new ConfirmEmailHandler(_identityService)
            .Handle(new ConfirmEmailCommand("not-a-guid", "token"), CancellationToken.None);

        await action.Should().ThrowAsync<EmailConfirmationFailedException>();
        await _identityService.DidNotReceive().ConfirmEmailAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
