using Insequens.Domain.Types;

namespace Insequens.Domain.Model.ToDoItem;

public record ToDoItemCreateModel(string Name, string? Description, int Priority,
    DateOnly? DueDate);
