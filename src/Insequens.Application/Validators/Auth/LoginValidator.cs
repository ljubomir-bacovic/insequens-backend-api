using FluentValidation;
using Insequens.Application.Commands.Auth;
using Insequens.Domain.Entities;

namespace Insequens.Application.Validators.Auth;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        RuleFor(x => x.DeviceName)
            .MaximumLength(RefreshToken.DeviceNameMaxLength)
            .WithMessage($"Device name must not exceed {RefreshToken.DeviceNameMaxLength} characters.");
    }
}
