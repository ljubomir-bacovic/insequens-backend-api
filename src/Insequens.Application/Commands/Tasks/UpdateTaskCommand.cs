using Insequens.Application.Authorization;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands.Tasks;

/// <summary>Changes only the fields that are present; see <see cref="UpdateTaskRequest"/>.</summary>
/// <param name="ExpectedVersion">The version the client last read (<c>If-Match</c>); null skips the check.</param>
public record UpdateTaskCommand(
    Guid ItemId,
    Guid UserId,
    string? Name,
    Optional<string?> Description,
    TaskPriority? Priority,
    Optional<DateOnly?> DueDate,
    byte[]? ExpectedVersion = null) : IRequest, IOwned<ToDoItemEntity>
{
    Guid IResourceRequest.ResourceId => ItemId;
}
