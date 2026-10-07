using Insequens.Application.Abstractions;
using Insequens.Application.Authorization;
using Insequens.Application.Commands.ToDoItem;
using MediatR;
using DomainPriority = Insequens.Domain.Types.TaskPriority;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.Tasks;

public class UpdateTaskHandler(IResourceContext<ToDoItemEntity> toDoItem, IApplicationDbContext dbContext)
    : IRequestHandler<UpdateTaskCommand>
{
    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var item = toDoItem.Resource;

        if (request.Name is not null)
        {
            item.Rename(request.Name);
        }

        if (request.Description.HasValue)
        {
            item.UpdateDescription(request.Description.Value);
        }

        if (request.Priority is { } priority)
        {
            item.ChangePriority((DomainPriority)priority);
        }

        if (request.DueDate.HasValue)
        {
            item.Reschedule(request.DueDate.Value);
        }

        await dbContext.SaveChangesAsync(item, request.ExpectedVersion, cancellationToken);
    }
}
