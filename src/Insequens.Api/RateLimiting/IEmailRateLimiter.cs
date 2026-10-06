using System.Threading.RateLimiting;

namespace Insequens.Api.RateLimiting;

/// <summary>Limits auth requests per email address, so rotating client IPs does not lift the limit.</summary>
public interface IEmailRateLimiter
{
    RateLimitLease Acquire(string email);
}
