using System.ComponentModel.DataAnnotations;

namespace Insequens.Api.RateLimiting;

public sealed record RateLimitingOptions : IValidatableObject
{
    public const string SectionName = "RateLimiting";

    /// <summary>Login, registration, token refresh and password reset: per client and per email address.</summary>
    public FixedWindowPolicyOptions Auth { get; init; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };

    /// <summary>POST, PATCH and DELETE on resources: per user.</summary>
    public TokenBucketPolicyOptions Write { get; init; } = new()
    {
        TokenLimit = 60,
        TokensPerPeriod = 60,
        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
    };

    /// <summary>Every request: per user, or per client IP when anonymous.</summary>
    public FixedWindowPolicyOptions Global { get; init; } = new() { PermitLimit = 300, Window = TimeSpan.FromMinutes(1) };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ValidatePolicy(nameof(Auth), Auth)
            .Concat(ValidatePolicy(nameof(Write), Write))
            .Concat(ValidatePolicy(nameof(Global), Global));

    private static IEnumerable<ValidationResult> ValidatePolicy(string name, object policy)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(policy, new ValidationContext(policy), results, validateAllProperties: true);

        return results.Select(result => new ValidationResult(
            $"RateLimiting:{name}: {result.ErrorMessage}",
            result.MemberNames.Select(member => $"{name}.{member}").ToArray()));
    }
}
