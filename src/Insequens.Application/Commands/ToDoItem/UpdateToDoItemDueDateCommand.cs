using Insequens.Application.Commands;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

// No FluentValidation validator: the authoritative docs define this field as a DateOnly API contract without any
// additional range constraint, so there is no extra shape/range rule for FluentValidation to enforce beyond model
// binding (see docs/insequens-v1-architecture-and-guidelines.md sections 12 and 17, and
// docs/insequens-v1-modernisation-plan.md Task 3.10).
public record UpdateToDoItemDueDateCommand(Guid ItemId, Guid UserId, DateOnly DueDate)
    : IRequest<Unit>, IOwned;
