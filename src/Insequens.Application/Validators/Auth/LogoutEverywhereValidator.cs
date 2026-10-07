using FluentValidation;
using Insequens.Application.Commands.Auth;

namespace Insequens.Application.Validators.Auth;

public class LogoutEverywhereValidator : AbstractValidator<LogoutEverywhereCommand>
{
    public LogoutEverywhereValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}
