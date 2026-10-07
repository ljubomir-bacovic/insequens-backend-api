using FluentValidation;
using Insequens.Application.Commands.Account;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Validators.Account;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword)
            .ValidNewPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password.");
    }
}
