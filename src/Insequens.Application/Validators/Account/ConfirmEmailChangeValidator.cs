using FluentValidation;
using Insequens.Application.Commands.Account;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Validators.Account;

public class ConfirmEmailChangeValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.NewEmail).ValidEmail();
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
    }
}
