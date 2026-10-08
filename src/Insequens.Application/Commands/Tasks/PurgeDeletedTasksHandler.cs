using Insequens.Application.Abstractions;
using Insequens.Application.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Insequens.Application.Commands.Tasks;

public class PurgeDeletedTasksHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<TaskTrashOptions> options,
    ILogger<PurgeDeletedTasksHandler> logger)
    : IRequestHandler<PurgeDeletedTasksCommand, int>
{
    public async Task<int> Handle(PurgeDeletedTasksCommand request, CancellationToken cancellationToken)
    {
        var deletedBefore = (timeProvider.GetUtcNow() - options.Value.Retention).UtcDateTime;

        // One set-based DELETE: purged rows are never loaded, so their version is not checked.
        var purged = await dbContext.ToDoItems
            .IgnoreQueryFilters()
            .Where(item => item.IsDeleted && item.DeletedOn < deletedBefore)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Purged {Count} tasks deleted before {DeletedBefore}", purged, deletedBefore);
        return purged;
    }
}
