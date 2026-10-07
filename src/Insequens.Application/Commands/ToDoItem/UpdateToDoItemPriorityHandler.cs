using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using Insequens.Application.Commands;
using Insequens.Application.Profiles;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemPriorityHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemPriorityCommand>
{
    public async Task Handle(
        UpdateToDoItemPriorityCommand request,
        CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        item.ChangePriority(request.Priority.ToDomain());
        await dbContext.SaveChangesAsync(item, request.ExpectedVersion, cancellationToken);
    }
}
