using FluentValidation;
using Insequens.Application.Commands.Account;
using Insequens.Application.Validators.Auth;

namespace Insequens.Application.Validators.Account;

public class RequestEmailChangeValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.NewEmail).ValidEmail();
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
    }
}
