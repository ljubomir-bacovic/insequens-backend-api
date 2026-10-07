using Insequens.Application.Commands;
using Insequens.Contracts.V1.Tasks;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record UpdateToDoItemPriorityCommand(Guid ItemId, Guid UserId, TaskPriority Priority)
    : IRequest<Unit>, IOwned;
