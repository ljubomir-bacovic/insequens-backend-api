using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class ConcurrencyConflictExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ConcurrencyConflictExceptionHandler> logger)
    : ExceptionProblemHandler<ConcurrencyConflictException>(problemDetailsService)
{
    protected override ProblemDetails CreateProblem(ConcurrencyConflictException exception)
    {
        logger.LogWarning("Concurrency conflict. Resource: {ResourceName}, Id: {ResourceId}", exception.ResourceName, exception.Id);

        return new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Type = ProblemTypes.ConcurrencyConflict,
            Title = "The resource was changed by another request.",
            Detail = "Read the resource again and retry. Send its ETag as If-Match to detect this before saving.",
        };
    }
}
