using Insequens.Application.Abstractions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Commands.Auth;
using Insequens.Application.Exceptions;
using Insequens.Application.Options;
using Insequens.Contracts.V1.Account;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Insequens.Application.Commands.Account;

/// <summary>
/// Disables the account at once and schedules it for <see cref="PurgeDeletedAccountsCommand"/> after the grace
/// period, during which support can still restore it.
/// </summary>
public class DeleteAccountHandler(
    IIdentityService identityService,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<AccountDeletionOptions> options,
    ILogger<DeleteAccountHandler> logger)
    : IRequestHandler<DeleteAccountCommand, AccountDeletionResponse>
{
    public async Task<AccountDeletionResponse> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        await CurrentPassword.VerifyAsync(identityService, request.UserId, request.CurrentPassword, cancellationToken);

        var now = timeProvider.GetUtcNow();
        if (!await identityService.MarkForDeletionAsync(request.UserId, now.UtcDateTime, cancellationToken))
        {
            throw new NotFoundException("Account", request.UserId);
        }

        await dbContext.RevokeRefreshTokensAsync(request.UserId, familyId: null, now.UtcDateTime, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account {UserId} scheduled for deletion", request.UserId);

        return new AccountDeletionResponse(now + options.Value.GracePeriod);
    }
}
