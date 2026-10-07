using FluentAssertions;
using Insequens.Application.Abstractions.Email;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Account;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V1.Auth;
using NSubstitute;

namespace Insequens.Application.Tests.Commands.Account;

public sealed class RequestEmailChangeHandlerTests : IDisposable
{
    private const string Password = "Current-Passw0rd";
    private const string NewEmail = "new@example.com";

    private readonly TestDbContextFactory _database = new();
    private readonly AccountTestServices _services = new();
    private readonly AuthUser _user = new(Guid.NewGuid(), "old@example.com");

    public RequestEmailChangeHandlerTests()
    {
        _services.IdentityService.FindByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _services.IdentityService.GenerateChangeEmailTokenAsync(_user.Id, NewEmail, Arg.Any<CancellationToken>()).Returns("change/token");
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithFreeAddress_SendsAConfirmationLinkToItAndANoticeToTheOldAddress()
    {
        _services.PasswordCheckReturns(_user.Id, Password, PasswordSignInStatus.Succeeded);

        var response = await SendAsync(new RequestEmailChangeCommand(_user.Id, NewEmail, Password));

        response.Should().BeSameAs(AccountResponses.EmailChangeRequested);
        var expectedLink = $"{AccountTestServices.FrontendBaseUrl}/confirm-email-change?userId={_user.Id}&email=new%40example.com&token=change%2Ftoken";
        await _services.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(message => message.To == NewEmail && message.TextBody!.EndsWith(expectedLink)),
            Arg.Any<CancellationToken>());
        await _services.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(message => message.To == _user.Email && !message.TextBody!.Contains("token")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_WithAddressOfAnotherAccount_ReturnsTheSameResponseWithoutSendingEmail()
    {
        _services.PasswordCheckReturns(_user.Id, Password, PasswordSignInStatus.Succeeded);
        _services.IdentityService.FindByEmailAsync(NewEmail, Arg.Any<CancellationToken>()).Returns(new AuthUser(Guid.NewGuid(), NewEmail));

        var response = await SendAsync(new RequestEmailChangeCommand(_user.Id, NewEmail, Password));

        response.Should().BeSameAs(AccountResponses.EmailChangeRequested);
        await _services.EmailSender.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default);
        await _services.IdentityService.DidNotReceiveWithAnyArgs().GenerateChangeEmailTokenAsync(default, default!, default);
    }

    [Fact]
    public async Task Send_WithTheCurrentAddress_SendsNothing()
    {
        _services.PasswordCheckReturns(_user.Id, Password, PasswordSignInStatus.Succeeded);

        var response = await SendAsync(new RequestEmailChangeCommand(_user.Id, "OLD@example.com", Password));

        response.Should().BeSameAs(AccountResponses.EmailChangeRequested);
        await _services.EmailSender.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default);
    }

    [Fact]
    public async Task Send_WithWrongPassword_ThrowsWithoutSendingEmail()
    {
        _services.PasswordCheckReturns(_user.Id, Password, PasswordSignInStatus.Failed);

        var action = () => SendAsync(new RequestEmailChangeCommand(_user.Id, NewEmail, Password));

        await action.Should().ThrowAsync<AccountUpdateFailedException>();
        await _services.EmailSender.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default);
    }

    private Task<AuthMessageResponse> SendAsync(RequestEmailChangeCommand command) =>
        _database.SendAsync(command, _services.Register);
}
