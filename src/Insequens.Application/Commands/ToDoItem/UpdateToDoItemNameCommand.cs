using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

public record UpdateToDoItemNameCommand(Guid ItemId, Guid UserId, string Name)
    : IRequest<Unit>, IOwned<ToDoItemEntity>
{
    Guid IResourceRequest.ResourceId => ItemId;
}
