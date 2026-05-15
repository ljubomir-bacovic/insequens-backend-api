using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Commands.ToDoItem;

public record CreateToDoItemCommand(
    string Name,
    string? Description,
    int Priority,
    DateOnly? DueDate,
    Guid UserId) : IRequest<ToDoItemGetDetailsModel>;
