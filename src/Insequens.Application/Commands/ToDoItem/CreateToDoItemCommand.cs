using MediatR;
using Insequens.Contracts.V1.Tasks;

namespace Insequens.Application.Commands.ToDoItem;

public record CreateToDoItemCommand(
    string Name,
    string? Description,
    int Priority,
    DateOnly? DueDate,
    Guid UserId) : IRequest<ToDoItemGetDetailsModel>;
