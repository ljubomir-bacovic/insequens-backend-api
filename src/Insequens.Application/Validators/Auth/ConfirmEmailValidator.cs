using FluentValidation;
using Insequens.Application.Commands.Auth;

namespace Insequens.Application.Validators.Auth;

public class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required.");
    }
}
