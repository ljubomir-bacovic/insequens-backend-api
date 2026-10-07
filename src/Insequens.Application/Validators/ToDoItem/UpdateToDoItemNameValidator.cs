using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class UpdateToDoItemNameValidator : AbstractValidator<UpdateToDoItemNameCommand>
{
    public UpdateToDoItemNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(ToDoItemEntity.NameMaxLength)
            .WithMessage($"Task name must not exceed {ToDoItemEntity.NameMaxLength} characters.");
    }
}
