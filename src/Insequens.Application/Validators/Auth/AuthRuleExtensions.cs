using FluentValidation;
using Insequens.Domain.Models.Auth;

namespace Insequens.Application.Validators.Auth;

internal static class AuthRuleExtensions
{
    private const int EmailMaximumLength = 256;

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(EmailMaximumLength).WithMessage($"Email must not exceed {EmailMaximumLength} characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

    // Mirrors the Identity password options (see PasswordPolicy) so Identity never rejects a password
    // after validation passed; otherwise the rejection would reveal that the email has no account.
    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .Length(PasswordPolicy.MinimumLength, PasswordPolicy.MaximumLength)
                .WithMessage($"Password must be between {PasswordPolicy.MinimumLength} and {PasswordPolicy.MaximumLength} characters.")
            .Must(password => password.Any(char.IsDigit)).WithMessage("Password must contain a digit.")
            .Must(password => password.Any(char.IsLower)).WithMessage("Password must contain a lowercase letter.")
            .Must(password => password.Any(char.IsUpper)).WithMessage("Password must contain an uppercase letter.")
            .Must(password => password.Any(character => !char.IsLetterOrDigit(character)))
                .WithMessage("Password must contain a character that is not a letter or digit.");
}
