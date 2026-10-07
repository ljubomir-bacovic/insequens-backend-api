namespace Insequens.Contracts.V1.Tasks;

public record ToDoItemGetListModel(
    Guid Id,
    string Name,
    string? Description,
    DateOnly? DueDate,
    bool IsCompleted,
    TaskPriority? Priority);
