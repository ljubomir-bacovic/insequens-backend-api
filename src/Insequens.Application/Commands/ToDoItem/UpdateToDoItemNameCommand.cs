using Insequens.Application.Commands;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record UpdateToDoItemNameCommand(Guid ItemId, Guid UserId, string Name)
    : IRequest<Unit>, IOwned;
