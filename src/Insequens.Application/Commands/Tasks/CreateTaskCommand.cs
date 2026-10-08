using Insequens.Application.Abstractions;
using Insequens.Contracts.V2.Tasks;
using MediatR;

namespace Insequens.Application.Commands.Tasks;

/// <param name="IdempotencyKey">The client's <c>Idempotency-Key</c>: a retry with it returns the first response.</param>
public record CreateTaskCommand(
    Guid UserId,
    string Name,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate,
    string? IdempotencyKey = null) : IRequest<TaskResponse>, IIdempotentRequest;
