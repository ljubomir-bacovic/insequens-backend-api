using Insequens.Application.Commands;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record DeleteToDoItemCommand(Guid ItemId, Guid UserId)
    : IRequest<Unit>, IOwned;
