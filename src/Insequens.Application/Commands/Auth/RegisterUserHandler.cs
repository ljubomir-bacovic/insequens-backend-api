using System.Net;
using Insequens.Application.Options;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Insequens.Application.Commands.Auth;

public class RegisterUserHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<RegisterUserHandler> logger)
    : IRequestHandler<RegisterUserCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            logger.LogInformation("Registration requested for existing user {UserId}", existingUser.Id);
            return AuthResponses.RegistrationAccepted;
        }

        var user = await identityService.CreateUserAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Identity rejected a registration that passed validation");
            return AuthResponses.RegistrationAccepted;
        }

        // The account exists from here on; finish sending the email even if the client disconnects.
        var token = await identityService.GenerateEmailConfirmationTokenAsync(user.Id, CancellationToken.None);
        var link = FrontendLink.Build(
            frontendOptions.Value.BaseUrl,
            "confirm-email",
            ("userId", user.Id.ToString()),
            ("token", token));

        await emailSender.SendEmailAsync(
            new EmailMessage(
                user.Email,
                "Please confirm your registration",
                $"<p>Please confirm your email by clicking <a href=\"{WebUtility.HtmlEncode(link)}\">here</a>.</p>",
                $"Please confirm your email by opening this link: {link}"),
            CancellationToken.None);

        logger.LogInformation("Registered user {UserId}", user.Id);

        return AuthResponses.RegistrationAccepted;
    }
}
