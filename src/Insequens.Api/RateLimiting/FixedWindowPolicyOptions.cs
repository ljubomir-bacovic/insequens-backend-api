using System.ComponentModel.DataAnnotations;

namespace Insequens.Api.RateLimiting;

public sealed record FixedWindowPolicyOptions
{
    [Range(1, 100_000)]
    public int PermitLimit { get; init; }

    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan Window { get; init; }
}
