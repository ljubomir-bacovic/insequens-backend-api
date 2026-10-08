using Insequens.Contracts.V1.Tasks;

namespace Insequens.Contracts.V1.Account;

/// <param name="DeletedOn">When the task was moved to the trash; null for a task that is not deleted.</param>
public sealed record ExportedTask(
    Guid Id,
    string Name,
    string? Description,
    TaskPriority? Priority,
    DateOnly? DueDate,
    bool IsCompleted,
    DateTimeOffset CreatedOn,
    DateTimeOffset UpdatedOn,
    DateTimeOffset? DeletedOn = null);
