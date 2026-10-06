using System.ComponentModel.DataAnnotations;

namespace Insequens.Infrastructure.DataAccess.Email;

public sealed record EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    [Required]
    public string SmtpServer { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool UseTls { get; init; } = true;

    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required]
    [EmailAddress]
    public string From { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Username))
        {
            yield break;
        }

        if (string.IsNullOrEmpty(Password))
        {
            yield return new ValidationResult(
                "Password is required when Username is set.",
                [nameof(Password)]);
        }

        if (!UseTls)
        {
            yield return new ValidationResult(
                "UseTls must be true when Username is set, so credentials are never sent unencrypted.",
                [nameof(UseTls)]);
        }
    }
}
