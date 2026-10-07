using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.ToDoItem;

/// <param name="ExpectedVersion">The version the client last read (<c>If-Match</c>); null skips the check.</param>
public record DeleteToDoItemCommand(Guid ItemId, Guid UserId, byte[]? ExpectedVersion = null)
    : IRequest, IOwned<ToDoItemEntity>
{
    Guid IResourceRequest.ResourceId => ItemId;
}
