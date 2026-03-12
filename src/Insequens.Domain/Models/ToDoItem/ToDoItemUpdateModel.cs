using Journal.Domain.Types;

namespace Journal.Domain.Model.ToDoItem;

public record ToDoItemUpdateModel(Guid Id, string Name, string? Description, TaskPriority? Priority,
    DateOnly? DueDate, bool IsCompleted);