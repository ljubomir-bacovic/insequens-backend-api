using FluentValidation;
using Insequens.Application.Commands.Account;

namespace Insequens.Application.Validators.Account;

public class DeleteAccountValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
    }
}
