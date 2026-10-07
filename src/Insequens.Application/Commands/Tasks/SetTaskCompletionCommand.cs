using Insequens.Application.Authorization;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.Tasks;

/// <param name="ExpectedVersion">The version the client last read (<c>If-Match</c>); null skips the check.</param>
public record SetTaskCompletionCommand(Guid ItemId, Guid UserId, bool Completed, byte[]? ExpectedVersion = null)
    : IRequest, IOwned<ToDoItemEntity>
{
    Guid IResourceRequest.ResourceId => ItemId;
}
