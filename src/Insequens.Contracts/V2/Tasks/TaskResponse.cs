namespace Insequens.Contracts.V2.Tasks;

/// <summary>A task, as returned by every v2 task endpoint.</summary>
public record TaskResponse(
    Guid Id,
    string Name,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate,
    bool IsCompleted);
