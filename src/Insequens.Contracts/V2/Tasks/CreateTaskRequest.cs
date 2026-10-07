namespace Insequens.Contracts.V2.Tasks;

public record CreateTaskRequest(
    string Name,
    string? Description = null,
    TaskPriority Priority = TaskPriority.None,
    DateOnly? DueDate = null);
