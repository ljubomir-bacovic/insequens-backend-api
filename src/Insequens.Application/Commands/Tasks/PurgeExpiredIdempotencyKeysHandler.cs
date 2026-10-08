using Insequens.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Insequens.Application.Commands.Tasks;

public class PurgeExpiredIdempotencyKeysHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<PurgeExpiredIdempotencyKeysHandler> logger)
    : IRequestHandler<PurgeExpiredIdempotencyKeysCommand, int>
{
    public async Task<int> Handle(PurgeExpiredIdempotencyKeysCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var purged = await dbContext.IdempotencyRecords
            .Where(record => record.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Purged {Count} expired idempotency keys", purged);
        return purged;
    }
}
