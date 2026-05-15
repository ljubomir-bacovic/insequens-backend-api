using FluentValidation;
using Insequens.Application.Commands.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class UpdateToDoItemPriorityValidator : AbstractValidator<UpdateToDoItemPriorityCommand>
{
    public UpdateToDoItemPriorityValidator()
    {
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Priority must be one of: 1 (high), 2 (medium), or 3 (low).");
    }
}
