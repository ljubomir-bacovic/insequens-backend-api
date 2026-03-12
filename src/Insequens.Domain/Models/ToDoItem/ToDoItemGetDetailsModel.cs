using Insequens.Domain.Types;

namespace Insequens.Domain.Model.ToDoItem;

public record ToDoItemGetDetailsModel(Guid Id, string Name, string? Description, TaskPriority? Priority,
    DateOnly? DueDate, bool IsCompleted);

