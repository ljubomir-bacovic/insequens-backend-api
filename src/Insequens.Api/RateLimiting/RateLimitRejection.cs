using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.RateLimiting;

public static class RateLimitRejection
{
    public static async Task WriteAsync(
        HttpContext context,
        TimeSpan? retryAfter,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitRejection).FullName!);

        logger.LogWarning(
            "Rate limit exceeded on {Path} for partition {PartitionKeyHash}",
            context.Request.Path.Value,
            RateLimitPartitionKey.Hash(partitionKey));

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/problem+json";

        if (retryAfter is { } delay)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds));
            context.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Detail = "Try again later.",
            Type = "Error",
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails), cancellationToken);
    }
}
