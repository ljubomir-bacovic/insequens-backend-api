using System.Threading.RateLimiting;
using Insequens.Domain.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Insequens.Api.RateLimiting;

/// <summary>Applies <see cref="IEmailRateLimiter"/> to an auth action whose body carries an email address.</summary>
public sealed class EmailRateLimitFilter(IEmailRateLimiter limiter) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var email = context.ActionArguments.Values.Select(GetEmail).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (email is null)
        {
            await next();
            return;
        }

        using var lease = limiter.Acquire(email);
        if (lease.IsAcquired)
        {
            await next();
            return;
        }

        TimeSpan? retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var delay) ? delay : null;
        await RateLimitRejection.WriteAsync(
            context.HttpContext,
            retryAfter,
            $"email:{email.Trim().ToUpperInvariant()}",
            context.HttpContext.RequestAborted);
        context.Result = new EmptyResult();
    }

    private static string? GetEmail(object? argument) => argument switch
    {
        LoginRequest request => request.Email,
        RegisterRequest request => request.Email,
        ForgotPasswordRequest request => request.Email,
        ResetPasswordRequest request => request.Email,
        _ => null,
    };
}
