namespace Insequens.Domain.Entities;

/// <summary>
/// A client's <c>Idempotency-Key</c> and the outcome of the request it was first sent with. A retry with the same key
/// and the same request gets the stored response instead of running again.
/// </summary>
public sealed class IdempotencyRecord : AuditableEntity
{
    public const int KeyMaxLength = 100;

    /// <summary>Hex of a SHA-256 hash.</summary>
    public const int RequestHashLength = 64;

    /// <summary>How long a key is remembered.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private IdempotencyRecord()
    {
    }

    public Guid UserId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;

    /// <summary>The JSON of the response; null while the first request is still running.</summary>
    public string? ResponseBody { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public bool IsCompleted => ResponseBody is not null;

    /// <summary>Claims the key for a request that is about to run.</summary>
    public static IdempotencyRecord Begin(Guid userId, string key, string requestHash, DateTime now)
    {
        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Key = key,
        };
        record.Restart(requestHash, now);

        return record;
    }

    public bool IsExpired(DateTime now) => ExpiresAt <= now;

    /// <summary>Whether a request with this hash is the same request the key was first sent with.</summary>
    public bool Matches(string requestHash) => string.Equals(RequestHash, requestHash, StringComparison.Ordinal);

    /// <summary>Reuses an expired key for a new request.</summary>
    public void Restart(string requestHash, DateTime now)
    {
        RequestHash = requestHash;
        ResponseBody = null;
        ExpiresAt = now + Retention;
    }

    public void Complete(string responseBody) => ResponseBody = responseBody;
}
