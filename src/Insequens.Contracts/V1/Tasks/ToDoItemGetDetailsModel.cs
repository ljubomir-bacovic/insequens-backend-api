namespace Insequens.Contracts.V1.Tasks;

public record ToDoItemGetDetailsModel(
    Guid Id,
    string Name,
    string? Description,
    TaskPriority? Priority,
    DateOnly? DueDate,
    bool IsCompleted);
