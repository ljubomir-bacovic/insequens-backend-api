using Insequens.Contracts.V2.Tasks;
using MediatR;

namespace Insequens.Application.Commands.Tasks;

public record CreateTaskCommand(
    Guid UserId,
    string Name,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate) : IRequest<TaskResponse>;
