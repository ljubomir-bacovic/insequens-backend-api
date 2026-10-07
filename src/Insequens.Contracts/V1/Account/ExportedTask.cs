using Insequens.Contracts.V1.Tasks;

namespace Insequens.Contracts.V1.Account;

public sealed record ExportedTask(
    Guid Id,
    string Name,
    string? Description,
    TaskPriority? Priority,
    DateOnly? DueDate,
    bool IsCompleted,
    DateTimeOffset CreatedOn,
    DateTimeOffset UpdatedOn);
