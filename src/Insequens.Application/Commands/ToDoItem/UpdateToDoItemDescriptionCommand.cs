using Insequens.Application.Commands;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

// No FluentValidation validator: the authoritative docs only require validators for documented user-controlled
// shape/range rules, and this API contract intentionally permits null/empty descriptions (see
// docs/insequens-v1-architecture-and-guidelines.md sections 12 and 17, and
// docs/insequens-v1-modernisation-plan.md Task 3.9).
public record UpdateToDoItemDescriptionCommand(Guid ItemId, Guid UserId, string? Description)
    : IRequest<Unit>, IOwned;
