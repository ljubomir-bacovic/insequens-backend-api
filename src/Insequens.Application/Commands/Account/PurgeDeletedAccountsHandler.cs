using Insequens.Application.Abstractions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Insequens.Application.Commands.Account;

public class PurgeDeletedAccountsHandler(
    IIdentityService identityService,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<AccountDeletionOptions> options,
    ILogger<PurgeDeletedAccountsHandler> logger)
    : IRequestHandler<PurgeDeletedAccountsCommand, int>
{
    public async Task<int> Handle(PurgeDeletedAccountsCommand request, CancellationToken cancellationToken)
    {
        var deletedBefore = (timeProvider.GetUtcNow() - options.Value.GracePeriod).UtcDateTime;
        var userIds = await identityService.FindAccountsDueForPurgeAsync(deletedBefore, cancellationToken);

        foreach (var userId in userIds)
        {
            // Removed explicitly rather than left to the database's cascade, so no provider keeps them.
            dbContext.ToDoItems.RemoveRange(
                await dbContext.ToDoItems.IgnoreQueryFilters().Where(item => item.UserId == userId).ToListAsync(cancellationToken));
            dbContext.RefreshTokens.RemoveRange(
                await dbContext.RefreshTokens.Where(token => token.UserId == userId).ToListAsync(cancellationToken));
            dbContext.IdempotencyRecords.RemoveRange(
                await dbContext.IdempotencyRecords.Where(record => record.UserId == userId).ToListAsync(cancellationToken));
            await dbContext.SaveChangesAsync(cancellationToken);

            await identityService.DeleteUserAsync(userId, cancellationToken);
            logger.LogInformation("Purged deleted account {UserId}", userId);
        }

        return userIds.Count;
    }
}
