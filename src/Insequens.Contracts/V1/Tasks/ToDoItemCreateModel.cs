namespace Insequens.Contracts.V1.Tasks;

public record ToDoItemCreateModel(string Name, string? Description, int Priority, DateOnly? DueDate);
