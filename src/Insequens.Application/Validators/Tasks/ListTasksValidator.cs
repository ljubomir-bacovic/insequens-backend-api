using FluentValidation;
using Insequens.Application.Queries.Tasks;

namespace Insequens.Application.Validators.Tasks;

public class ListTasksValidator : AbstractValidator<ListTasksQuery>
{
    public const int SearchMaxLength = 100;

    public ListTasksValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(SearchMaxLength)
            .WithMessage($"Search must not exceed {SearchMaxLength} characters.");

        RuleFor(x => x.SortBy).IsInEnum().WithMessage("SortBy must be one of: dueDate, priority, createdOn or name.");

        RuleFor(x => x.SortDirection).IsInEnum().WithMessage("SortDirection must be asc or desc.");

        RuleFor(x => x.Priority).IsInEnum().WithMessage("Priority must be one of: none, low, medium or high.");

        RuleFor(x => x.DueTo)
            .GreaterThanOrEqualTo(x => x.DueFrom)
            .WithMessage("DueTo must not be before DueFrom.")
            .When(x => x.DueFrom is not null && x.DueTo is not null);
    }
}
