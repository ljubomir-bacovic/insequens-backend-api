using Insequens.Domain.Types;

namespace Insequens.Domain.Model.ToDoItem;

public record ToDoItemUpdateModel(Guid Id, string Name, string? Description, TaskPriority? Priority,
    DateOnly? DueDate, bool IsCompleted);