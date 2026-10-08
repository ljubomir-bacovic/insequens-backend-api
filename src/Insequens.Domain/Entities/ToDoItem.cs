using Insequens.Domain.Exceptions;
using Insequens.Domain.Types;

namespace Insequens.Domain.Entities;

public class ToDoItem : AuditableEntity, IOwnedEntity, ISoftDeletable
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    private ToDoItem()
    {
    }

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedOn { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>Changes on every save; the API exposes it as the ETag for optimistic concurrency.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public static ToDoItem Create(Guid userId, string name, string? description, TaskPriority priority, DateOnly? dueDate)
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

    public void ChangePriority(TaskPriority priority) => Priority = priority;

    public void Reschedule(DateOnly? dueDate) => DueDate = dueDate;

    public void MarkCompleted() => IsCompleted = true;

    public void Reopen() => IsCompleted = false;

    /// <summary>Moves the task to the trash. Deleting a deleted task changes nothing.</summary>
    public void Delete() => IsDeleted = true;

    /// <summary>Takes the task out of the trash. Restoring a task that is not deleted changes nothing.</summary>
    public void Restore() => IsDeleted = false;
}
