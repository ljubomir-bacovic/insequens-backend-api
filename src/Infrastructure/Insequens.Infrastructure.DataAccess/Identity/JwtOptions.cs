using System.ComponentModel.DataAnnotations;

namespace Insequens.Infrastructure.DataAccess.Identity;

public sealed record JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";
    public const int MinimumSecretLength = 32;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>A single signing key. Use either <see cref="Key"/> or <see cref="Keys"/>, not both.</summary>
    public string? Key { get; init; }

    /// <summary>Signing keys for rotation. Every key validates tokens; the newest active key signs them.</summary>
    public IList<JwtSigningKey> Keys { get; init; } = [];

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    [Range(typeof(TimeSpan), "00:01:00", "365.00:00:00")]
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Keys.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(Key) || Key.Length < MinimumSecretLength)
            {
                yield return new ValidationResult(
                    $"Jwt:Key is required and must be at least {MinimumSecretLength} characters when Jwt:Keys is empty.",
                    [nameof(Key)]);
            }

            yield break;
        }

        if (!string.IsNullOrEmpty(Key))
        {
            yield return new ValidationResult("Set either Jwt:Key or Jwt:Keys, not both.", [nameof(Key), nameof(Keys)]);
        }

        for (var index = 0; index < Keys.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(Keys[index].Id))
            {
                yield return new ValidationResult($"Jwt:Keys:{index}:Id is required.", [nameof(Keys)]);
            }

            if (string.IsNullOrWhiteSpace(Keys[index].Secret) || Keys[index].Secret.Length < MinimumSecretLength)
            {
                yield return new ValidationResult(
                    $"Jwt:Keys:{index}:Secret is required and must be at least {MinimumSecretLength} characters.",
                    [nameof(Keys)]);
            }
        }

        var duplicateIds = Keys
            .GroupBy(key => key.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateIds.Length > 0)
        {
            yield return new ValidationResult(
                $"Jwt:Keys must have unique Ids. Duplicated: {string.Join(", ", duplicateIds)}.",
                [nameof(Keys)]);
        }
    }
}
