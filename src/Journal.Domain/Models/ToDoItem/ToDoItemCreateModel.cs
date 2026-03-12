using Journal.Domain.Types;

namespace Journal.Domain.Model.ToDoItem;

public record ToDoItemCreateModel(string Name, string? Description, int Priority,
    DateOnly? DueDate);
