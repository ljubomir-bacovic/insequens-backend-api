using Insequens.Application.Commands;
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Queries.ToDoItem;

// No FluentValidation validator: this single-item query carries only route/JWT identifiers. Route constraints
// enforce Guid shape, and IOwned + OwnershipBehavior enforce existence/authorization before the handler executes
// (see docs/insequens-v1-architecture-and-guidelines.md sections 8.2, 10.3, and 12).
public record GetToDoItemQuery(Guid ItemId, Guid UserId)
    : IRequest<ToDoItemGetDetailsModel>, IOwned;
