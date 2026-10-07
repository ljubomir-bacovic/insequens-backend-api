using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class CreateToDoItemValidator : AbstractValidator<CreateToDoItemCommand>
{
    public CreateToDoItemValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(ToDoItemEntity.NameMaxLength)
            .WithMessage($"Task name must not exceed {ToDoItemEntity.NameMaxLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(ToDoItemEntity.DescriptionMaxLength)
            .WithMessage($"Task description must not exceed {ToDoItemEntity.DescriptionMaxLength} characters.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 3)
            .WithMessage("Priority must be one of: 0 (none), 1 (high), 2 (medium), or 3 (low).");

        RuleFor(x => x.DueDate).WithinDueDateRange(timeProvider);
    }
}
