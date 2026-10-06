using System.Net;
using Insequens.Application.Options;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Insequens.Application.Commands.Auth;

public class ForgotPasswordHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<ForgotPasswordHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Password reset requested for an email address without an account");
            return AuthResponses.PasswordResetRequested;
        }

        var token = await identityService.GeneratePasswordResetTokenAsync(user.Id, cancellationToken);
        var link = FrontendLink.Build(
            frontendOptions.Value.BaseUrl,
            "reset-password",
            ("token", token),
            ("email", user.Email));

        await emailSender.SendEmailAsync(
            new EmailMessage(
                user.Email,
                "Password Reset",
                $"<p>Click <a href=\"{WebUtility.HtmlEncode(link)}\">here</a> to reset your password.</p>",
                $"Open this link to reset your password: {link}"),
            cancellationToken);

        logger.LogInformation("Password reset requested for user {UserId}", user.Id);

        return AuthResponses.PasswordResetRequested;
    }
}
