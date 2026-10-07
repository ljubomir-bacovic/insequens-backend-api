using FluentAssertions;
using Insequens.Api.RateLimiting;
using Microsoft.Extensions.Options;

namespace Insequens.Api.Tests.RateLimiting;

public class EmailRateLimiterTests
{
    [Fact]
    public void Acquire_OverPermitLimit_IsRejectedForThatEmailOnly()
    {
        using var limiter = new EmailRateLimiter(Options.Create(new RateLimitingOptions
        {
            Auth = new FixedWindowPolicyOptions { PermitLimit = 2, Window = TimeSpan.FromMinutes(1) },
        }));

        using var first = limiter.Acquire("user@example.com");
        using var second = limiter.Acquire(" USER@example.com ");
        using var third = limiter.Acquire("user@EXAMPLE.com");
        using var otherEmail = limiter.Acquire("other@example.com");

        first.IsAcquired.Should().BeTrue();
        second.IsAcquired.Should().BeTrue();
        third.IsAcquired.Should().BeFalse("case and surrounding spaces do not create a new partition");
        otherEmail.IsAcquired.Should().BeTrue();
    }
}
