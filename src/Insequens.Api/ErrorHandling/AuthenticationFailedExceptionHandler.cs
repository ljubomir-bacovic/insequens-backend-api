using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class AuthenticationFailedExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<AuthenticationFailedExceptionHandler> logger)
    : ExceptionProblemHandler<AuthenticationFailedException>(problemDetailsService)
{
    protected override ProblemDetails CreateProblem(AuthenticationFailedException exception)
    {
        // One body for every reason, so a client cannot tell an unknown email from a wrong password,
        // an unconfirmed account or a lockout.
        logger.LogInformation("Authentication failed.");

        return new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Type = ProblemTypes.AuthenticationFailed,
            Title = "Authentication failed.",
        };
    }
}
