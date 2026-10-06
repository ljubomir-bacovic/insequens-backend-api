using FluentValidation;
using Insequens.Application.Commands.Auth;

namespace Insequens.Application.Validators.Auth;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).ValidNewPassword();
    }
}
