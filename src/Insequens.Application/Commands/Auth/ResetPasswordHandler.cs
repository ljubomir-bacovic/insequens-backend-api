using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Insequens.Application.Commands.Auth;

public class ResetPasswordHandler(IIdentityService identityService, ILogger<ResetPasswordHandler> logger)
    : IRequestHandler<ResetPasswordCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Password reset attempted for an email address without an account");
            return AuthResponses.PasswordResetAccepted;
        }

        if (!await identityService.ResetPasswordAsync(user.Id, request.Token, request.NewPassword, cancellationToken))
        {
            logger.LogInformation("Password reset failed for user {UserId}", user.Id);
            return AuthResponses.PasswordResetAccepted;
        }

        await identityService.RevokeRefreshTokenAsync(user.Id, cancellationToken);
        logger.LogInformation("Password reset for user {UserId}", user.Id);

        return AuthResponses.PasswordResetAccepted;
    }
}
