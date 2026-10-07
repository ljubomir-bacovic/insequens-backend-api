using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class ForbiddenExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ForbiddenExceptionHandler> logger)
    : ExceptionProblemHandler<ForbiddenException>(problemDetailsService)
{
    protected override ProblemDetails CreateProblem(ForbiddenException exception)
    {
        logger.LogWarning("Forbidden: the {RequiredRole} role is required", exception.RequiredRole);

        return new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Type = ProblemTypes.Forbidden,
            Title = "Access denied.",
        };
    }
}
