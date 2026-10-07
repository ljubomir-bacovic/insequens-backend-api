using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class EmailConfirmationFailedExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<EmailConfirmationFailedExceptionHandler> logger)
    : ExceptionProblemHandler<EmailConfirmationFailedException>(problemDetailsService)
{
    protected override ProblemDetails CreateProblem(EmailConfirmationFailedException exception)
    {
        logger.LogInformation("Email confirmation failed.");

        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = ProblemTypes.EmailConfirmationFailed,
            Title = "Email confirmation failed.",
        };
    }
}
