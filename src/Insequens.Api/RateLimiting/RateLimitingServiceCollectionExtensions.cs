using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Insequens.Api.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEmailRateLimiter, EmailRateLimiter>();

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingOptions>>((limiter, settings) =>
            {
                var options = settings.Value;

                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        RateLimitPartitionKey.For(context),
                        _ => ToFixedWindow(options.Global)));

                limiter.AddPolicy(RateLimitPolicies.Auth, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        RateLimitPartitionKey.For(context),
                        _ => ToFixedWindow(options.Auth)));

                limiter.AddPolicy(RateLimitPolicies.Write, context =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        RateLimitPartitionKey.For(context),
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = options.Write.TokenLimit,
                            TokensPerPeriod = options.Write.TokensPerPeriod,
                            ReplenishmentPeriod = options.Write.ReplenishmentPeriod,
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }));

                limiter.OnRejected = (context, cancellationToken) =>
                {
                    TimeSpan? retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay) ? delay : null;

                    return new ValueTask(RateLimitRejection.WriteAsync(
                        context.HttpContext,
                        retryAfter,
                        RateLimitPartitionKey.For(context.HttpContext),
                        cancellationToken));
                };
            });

        return services;
    }

    private static FixedWindowRateLimiterOptions ToFixedWindow(FixedWindowPolicyOptions policy) => new()
    {
        PermitLimit = policy.PermitLimit,
        Window = policy.Window,
        QueueLimit = 0,
        AutoReplenishment = true,
    };
}
