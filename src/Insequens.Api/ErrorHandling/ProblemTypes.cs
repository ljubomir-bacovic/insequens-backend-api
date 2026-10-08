namespace Insequens.Api.ErrorHandling;

/// <summary>
/// Stable <c>type</c> values of the ProblemDetails this API returns. Clients branch on these, not on titles,
/// so a value never changes once published.
/// </summary>
public static class ProblemTypes
{
    private const string Prefix = "urn:insequens:error:";

    public const string NotFound = Prefix + "not-found";
    public const string Forbidden = Prefix + "forbidden";
    public const string AuthenticationFailed = Prefix + "authentication-failed";
    public const string EmailConfirmationFailed = Prefix + "email-confirmation-failed";
    public const string AccountUpdateFailed = Prefix + "account-update-failed";
    public const string DomainRuleViolated = Prefix + "domain-rule-violated";
    public const string Validation = Prefix + "validation";
    public const string PreconditionFailed = Prefix + "precondition-failed";
    public const string ConcurrencyConflict = Prefix + "concurrency-conflict";
    public const string IdempotencyKeyReused = Prefix + "idempotency-key-reused";
    public const string IdempotentRequestInProgress = Prefix + "idempotent-request-in-progress";
    public const string RateLimited = Prefix + "rate-limited";
    public const string Internal = Prefix + "internal";
}
