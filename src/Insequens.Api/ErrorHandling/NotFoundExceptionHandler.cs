using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class NotFoundExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<NotFoundExceptionHandler> logger)
    : ExceptionProblemHandler<NotFoundException>(problemDetailsService)
{
    protected override ProblemDetails CreateProblem(NotFoundException exception)
    {
        // Covers both a missing resource and one the caller does not own, so IDs cannot be probed.
        logger.LogWarning("Resource not found. Resource: {ResourceName}, Id: {ResourceId}", exception.ResourceName, exception.Id);

        return new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Type = ProblemTypes.NotFound,
            Title = $"{exception.ResourceName} for id {exception.Id} not found.",
        };
    }
}
