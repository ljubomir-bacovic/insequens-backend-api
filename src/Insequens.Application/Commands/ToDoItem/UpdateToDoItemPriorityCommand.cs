using Insequens.Application.Commands;
using Insequens.Domain.Types;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record UpdateToDoItemPriorityCommand(Guid ItemId, Guid UserId, TaskPriority Priority)
    : IRequest<Unit>, IOwned;
