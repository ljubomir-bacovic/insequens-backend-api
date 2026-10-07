using Insequens.Application.Abstractions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Insequens.Application.Commands.Account;

public class ChangePasswordHandler(
    IIdentityService identityService,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ChangePasswordHandler> logger)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        await CurrentPassword.VerifyAsync(identityService, request.UserId, request.CurrentPassword, cancellationToken);

        if (!await identityService.ChangePasswordAsync(
                request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken))
        {
            throw new AccountUpdateFailedException("The new password was rejected.");
        }

        // A new password ends every session, including one an attacker may hold.
        await dbContext.RevokeRefreshTokensAsync(
            request.UserId, familyId: null, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Password changed for user {UserId}", request.UserId);
    }
}
