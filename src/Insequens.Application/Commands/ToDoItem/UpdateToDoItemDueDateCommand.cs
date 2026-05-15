using Insequens.Application.Commands;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record UpdateToDoItemDueDateCommand(Guid ItemId, Guid UserId, DateOnly DueDate)
    : IRequest<Unit>, IOwned;
