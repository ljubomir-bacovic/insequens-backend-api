using FluentValidation;
using Insequens.Application.Commands.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class UpdateToDoItemNameValidator : AbstractValidator<UpdateToDoItemNameCommand>
{
    public UpdateToDoItemNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(200).WithMessage("Task name must not exceed 200 characters.");
    }
}
