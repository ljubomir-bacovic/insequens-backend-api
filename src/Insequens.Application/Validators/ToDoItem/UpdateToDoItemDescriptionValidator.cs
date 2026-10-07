using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Validators.ToDoItem;

public class UpdateToDoItemDescriptionValidator : AbstractValidator<UpdateToDoItemDescriptionCommand>
{
    public UpdateToDoItemDescriptionValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(ToDoItemEntity.DescriptionMaxLength)
            .WithMessage($"Task description must not exceed {ToDoItemEntity.DescriptionMaxLength} characters.");
    }
}
