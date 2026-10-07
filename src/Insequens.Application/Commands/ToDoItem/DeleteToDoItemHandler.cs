using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class DeleteToDoItemHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<DeleteToDoItemCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteToDoItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        dbContext.ToDoItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
