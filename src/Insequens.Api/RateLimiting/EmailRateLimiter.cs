using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Insequens.Api.RateLimiting;

public sealed class EmailRateLimiter : IEmailRateLimiter, IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    public EmailRateLimiter(IOptions<RateLimitingOptions> options)
    {
        var auth = options.Value.Auth;

        _limiter = PartitionedRateLimiter.Create<string, string>(email => RateLimitPartition.GetFixedWindowLimiter(
            email,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = auth.PermitLimit,
                Window = auth.Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    public RateLimitLease Acquire(string email) =>
        _limiter.AttemptAcquire(email.Trim().ToUpperInvariant());

    public void Dispose() => _limiter.Dispose();
}
