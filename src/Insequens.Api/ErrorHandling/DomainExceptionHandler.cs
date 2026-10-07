using Insequens.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<DomainExceptionHandler> logger)
    : ExceptionProblemHandler<DomainException>(problemDetailsService)
{
    protected override ProblemDetails CreateProblem(DomainException exception)
    {
        logger.LogWarning("Domain rule violated. Rule: {DomainRule}", exception.GetType().Name);

        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = ProblemTypes.DomainRuleViolated,
            Title = "Domain rule violated.",
            Detail = exception.Message,
        };
    }
}
