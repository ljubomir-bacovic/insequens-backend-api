using System.ComponentModel.DataAnnotations;

namespace Insequens.Api.RateLimiting;

public sealed record TokenBucketPolicyOptions
{
    [Range(1, 100_000)]
    public int TokenLimit { get; init; }

    [Range(1, 100_000)]
    public int TokensPerPeriod { get; init; }

    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan ReplenishmentPeriod { get; init; }
}
