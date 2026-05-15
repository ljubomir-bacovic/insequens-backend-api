using FluentValidation;
using Insequens.Application.Commands.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class CreateToDoItemValidator : AbstractValidator<CreateToDoItemCommand>
{
    public CreateToDoItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(200).WithMessage("Task name must not exceed 200 characters.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 3)
            .WithMessage("Priority must be between 0 (none) and 3 (low).");
    }
}
