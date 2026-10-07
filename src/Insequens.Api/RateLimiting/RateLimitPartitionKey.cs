using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Insequens.Api.RateLimiting;

public static class RateLimitPartitionKey
{
    /// <summary>
    /// The user ID for an authenticated request, otherwise the client IP. The IP honours
    /// <c>X-Forwarded-For</c> only when the forwarded headers middleware trusts the proxy.
    /// </summary>
    public static string For(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    /// <summary>A short, stable hash so logs can correlate rejections without holding IPs or emails.</summary>
    public static string Hash(string partitionKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(partitionKey)))[..16];
}
