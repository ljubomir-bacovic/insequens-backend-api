using Insequens.Application.Abstractions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using Insequens.Contracts.V1.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Insequens.Application.Commands.Account;

public class ConfirmEmailChangeHandler(
    IIdentityService identityService,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ConfirmEmailChangeHandler> logger)
    : IRequestHandler<ConfirmEmailChangeCommand, AuthMessageResponse>
{
    public async Task<AuthMessageResponse> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var userId)
            || !await identityService.ChangeEmailAsync(userId, request.NewEmail, request.Token, cancellationToken))
        {
            throw new AccountUpdateFailedException("The email change link is invalid or has expired.");
        }

        // The sign-in name changed, so every session signs in again with it.
        await dbContext.RevokeRefreshTokensAsync(userId, familyId: null, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Email changed for user {UserId}", userId);

        return AccountResponses.EmailChanged;
    }
}
