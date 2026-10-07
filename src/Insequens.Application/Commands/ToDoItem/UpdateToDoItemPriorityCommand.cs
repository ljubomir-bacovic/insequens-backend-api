using Insequens.Application.Authorization;
using Insequens.Contracts.V1.Tasks;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public record UpdateToDoItemPriorityCommand(Guid ItemId, Guid UserId, TaskPriority Priority)
    : IRequest, IOwned<ToDoItemEntity>
{
    Guid IResourceRequest.ResourceId => ItemId;
}
