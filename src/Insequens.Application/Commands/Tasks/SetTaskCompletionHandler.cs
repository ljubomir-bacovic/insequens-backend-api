using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.Tasks;

/// <summary>Idempotent: setting the state a task already has changes nothing.</summary>
public class SetTaskCompletionHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<SetTaskCompletionCommand>
{
    public async Task Handle(SetTaskCompletionCommand request, CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        if (request.Completed)
        {
            item.MarkCompleted();
        }
        else
        {
            item.Reopen();
        }

        await dbContext.SaveChangesAsync(item, request.ExpectedVersion, cancellationToken);
    }
}
