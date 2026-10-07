using Insequens.Application.Abstractions;
using Insequens.Application.Abstractions.Identity;
using Insequens.Application.Exceptions;
using Insequens.Contracts.V1.Account;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Insequens.Application.Profiles;

namespace Insequens.Application.Queries.Account;

public class ExportUserDataHandler(
    IIdentityService identityService,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider)
    : IRequestHandler<ExportUserDataQuery, UserDataExport>
{
    public async Task<UserDataExport> Handle(ExportUserDataQuery request, CancellationToken cancellationToken)
    {
        var account = await identityService.GetAccountAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Account", request.UserId);

        var tasks = await dbContext.ToDoItems.AsNoTracking()
            .Where(item => item.UserId == request.UserId)
            .OrderBy(item => item.CreatedOn)
            .ToListAsync(cancellationToken);
        var sessions = await dbContext.RefreshTokens.AsNoTracking()
            .Where(token => token.UserId == request.UserId)
            .OrderBy(token => token.CreatedOn)
            .ToListAsync(cancellationToken);

        return new UserDataExport(
            timeProvider.GetUtcNow(),
            new ExportedAccount(account.Id, account.Email, account.EmailConfirmed, account.Roles),
            [
                .. tasks.Select(item => new ExportedTask(
                    item.Id,
                    item.Name,
                    item.Description,
                    item.Priority.ToV1(),
                    item.DueDate,
                    item.IsCompleted,
                    Utc(item.CreatedOn),
                    Utc(item.UpdatedOn))),
            ],
            [
                .. sessions.Select(token => new ExportedSession(
                    token.FamilyId,
                    token.DeviceName,
                    token.CreatedByIp,
                    Utc(token.CreatedOn),
                    Utc(token.ExpiresAt),
                    token.RevokedAt is { } revokedAt ? Utc(revokedAt) : null)),
            ]);
    }

    // Stored times are UTC; the database returns them without a kind.
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
