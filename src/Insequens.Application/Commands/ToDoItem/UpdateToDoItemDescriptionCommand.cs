using Insequens.Application.Commands;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record UpdateToDoItemDescriptionCommand(Guid ItemId, Guid UserId, string Description)
    : IRequest<Unit>, IOwned;
