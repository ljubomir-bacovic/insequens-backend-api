using Insequens.Application.Commands;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

// No FluentValidation validator: this request carries only route/JWT identifiers. Route constraints enforce Guid
// shape, and IOwned + OwnershipBehavior enforce existence/authorization for specific-resource requests (see
// docs/insequens-v1-architecture-and-guidelines.md sections 8.2, 10.3, and 12).
public record DeleteToDoItemCommand(Guid ItemId, Guid UserId)
    : IRequest<Unit>, IOwned;
