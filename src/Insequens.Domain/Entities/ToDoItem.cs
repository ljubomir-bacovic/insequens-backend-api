using Insequens.Domain.Exceptions;
using Insequens.Domain.Types;

namespace Insequens.Domain.Entities;

public class ToDoItem : AuditableEntity, IOwnedEntity
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    private ToDoItem()
    {
    }

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskPriority? Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }

    /// <summary>Changes on every save; the API exposes it as the ETag for optimistic concurrency.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public static ToDoItem Create(Guid userId, string name, string? description, TaskPriority? priority, DateOnly? dueDate)
    {
        var item = new ToDoItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
        };

        item.Rename(name);
        item.UpdateDescription(description);
        item.ChangePriority(priority);
        item.Reschedule(dueDate);

        return item;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidToDoItemNameException("Task name is required.");
        }

        if (name.Length > NameMaxLength)
        {
            throw new InvalidToDoItemNameException($"Task name must not exceed {NameMaxLength} characters.");
        }

        Name = name;
    }

    public void UpdateDescription(string? description)
    {
        if (description?.Length > DescriptionMaxLength)
        {
            throw new ToDoItemDescriptionTooLongException(DescriptionMaxLength);
        }

        Description = description;
    }

    public void ChangePriority(TaskPriority? priority) => Priority = priority;

    public void Reschedule(DateOnly? dueDate) => DueDate = dueDate;

    public void MarkCompleted() => IsCompleted = true;

    public void Reopen() => IsCompleted = false;
}
