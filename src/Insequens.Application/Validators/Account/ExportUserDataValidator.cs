using FluentValidation;
using Insequens.Application.Queries.Account;

namespace Insequens.Application.Validators.Account;

public class ExportUserDataValidator : AbstractValidator<ExportUserDataQuery>
{
    public ExportUserDataValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}
