namespace Insequens.Contracts.V2.Tasks;

/// <summary>
/// A partial update: only the fields present in the body change. <c>Name</c> and <c>Priority</c> cannot be
/// cleared, so null means "unchanged"; <c>Description</c> and <c>DueDate</c> can, so they are
/// <see cref="Optional{T}"/> and an explicit null clears them. A property missing from the body binds as absent.
/// </summary>
public record UpdateTaskRequest(
    string? Name,
    Optional<string?> Description,
    TaskPriority? Priority,
    Optional<DateOnly?> DueDate);
