using FluentValidation;
using Insequens.Application.Commands.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class UpdateToDoItemDueDateValidator : AbstractValidator<UpdateToDoItemDueDateCommand>
{
    public UpdateToDoItemDueDateValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.DueDate).WithinDueDateRange(timeProvider);
    }
}
