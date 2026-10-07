using System.Net;
using Insequens.Application.Abstractions.Email;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using Insequens.Application.Options;
using Insequens.Contracts.V1.Auth;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Insequens.Application.Commands.Account;

public class RequestEmailChangeHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<RequestEmailChangeHandler> logger)
    : IRequestHandler<RequestEmailChangeCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        await CurrentPassword.VerifyAsync(identityService, request.UserId, request.CurrentPassword, cancellationToken);

        var user = await identityService.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Account", request.UserId);

        if (string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase)
            || await identityService.FindByEmailAsync(request.NewEmail, cancellationToken) is not null)
        {
            logger.LogInformation("Email change for user {UserId} requested to an address that is already in use", user.Id);
            return AccountResponses.EmailChangeRequested;
        }

        // From here on the request is valid; finish sending both emails even if the client disconnects.
        var token = await identityService.GenerateChangeEmailTokenAsync(user.Id, request.NewEmail, CancellationToken.None);
        var link = FrontendLink.Build(
            frontendOptions.Value.BaseUrl,
            "confirm-email-change",
            ("userId", user.Id.ToString()),
            ("email", request.NewEmail),
            ("token", token));

        await emailSender.SendEmailAsync(
            new EmailMessage(
                request.NewEmail,
                "Confirm your new email address",
                $"<p>Confirm your new email address by clicking <a href=\"{WebUtility.HtmlEncode(link)}\">here</a>.</p>",
                $"Confirm your new email address by opening this link: {link}"),
            CancellationToken.None);
        await emailSender.SendEmailAsync(
            new EmailMessage(
                user.Email,
                "A change of your email address was requested",
                "<p>Someone asked to move your Insequens account to a new email address. "
                    + "If this was not you, change your password now.</p>",
                "Someone asked to move your Insequens account to a new email address. "
                    + "If this was not you, change your password now."),
            CancellationToken.None);

        logger.LogInformation("Email change requested for user {UserId}", user.Id);

        return AccountResponses.EmailChangeRequested;
    }
}
