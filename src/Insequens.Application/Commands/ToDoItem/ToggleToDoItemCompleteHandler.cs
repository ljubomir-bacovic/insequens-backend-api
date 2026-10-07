using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class ToggleToDoItemCompleteHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<ToggleToDoItemCompleteCommand, Unit>
{
    public async Task<Unit> Handle(
        ToggleToDoItemCompleteCommand request,
        CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        if (item.IsCompleted)
        {
            item.Reopen();
        }
        else
        {
            item.MarkCompleted();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
