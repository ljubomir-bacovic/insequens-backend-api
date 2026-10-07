using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public class UpdateToDoItemDueDateHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<UpdateToDoItemDueDateCommand>
{
    public async Task Handle(
        UpdateToDoItemDueDateCommand request,
        CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        item.Reschedule(request.DueDate);
        await dbContext.SaveChangesAsync(item, request.ExpectedVersion, cancellationToken);
    }
}
