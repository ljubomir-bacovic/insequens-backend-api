using FluentValidation;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Validators.ToDoItem;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Validators.Tasks;

/// <summary>Validates only the fields present in the PATCH.</summary>
public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(ToDoItemEntity.NameMaxLength)
            .WithMessage($"Task name must not exceed {ToDoItemEntity.NameMaxLength} characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description.Value)
            .MaximumLength(ToDoItemEntity.DescriptionMaxLength)
            .WithMessage($"Task description must not exceed {ToDoItemEntity.DescriptionMaxLength} characters.")
            .OverridePropertyName(nameof(UpdateTaskCommand.Description))
            .When(x => x.Description.HasValue);

        RuleFor(x => x.Priority).IsInEnum().WithMessage("Priority must be one of: none, low, medium or high.");

        RuleFor(x => x.DueDate.Value)
            .WithinDueDateRange(timeProvider)
            .OverridePropertyName(nameof(UpdateTaskCommand.DueDate))
            .When(x => x.DueDate.HasValue);
    }
}
