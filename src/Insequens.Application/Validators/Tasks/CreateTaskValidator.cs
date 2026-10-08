using FluentValidation;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Validators.ToDoItem;
using Insequens.Domain.Entities;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Validators.Tasks;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(ToDoItemEntity.NameMaxLength)
            .WithMessage($"Task name must not exceed {ToDoItemEntity.NameMaxLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(ToDoItemEntity.DescriptionMaxLength)
            .WithMessage($"Task description must not exceed {ToDoItemEntity.DescriptionMaxLength} characters.");

        RuleFor(x => x.Priority).IsInEnum().WithMessage("Priority must be one of: none, low, medium or high.");

        RuleFor(x => x.DueDate).WithinDueDateRange(timeProvider);

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(IdempotencyRecord.KeyMaxLength)
            .WithMessage($"Idempotency-Key must be 1 to {IdempotencyRecord.KeyMaxLength} characters.")
            .When(x => x.IdempotencyKey is not null);
    }
}
