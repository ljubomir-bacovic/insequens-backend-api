using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class PreconditionFailedExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<PreconditionFailedExceptionHandler> logger)
    : ExceptionProblemHandler<PreconditionFailedException>(problemDetailsService)
{
    public const string Title = "The resource has changed.";

    protected override ProblemDetails CreateProblem(PreconditionFailedException exception)
    {
        logger.LogInformation("Precondition failed. Resource: {ResourceName}, Id: {ResourceId}", exception.ResourceName, exception.Id);

        return new ProblemDetails
        {
            Status = StatusCodes.Status412PreconditionFailed,
            Type = ProblemTypes.PreconditionFailed,
            Title = Title,
            Detail = "The If-Match version is not the current one. Read the resource again and retry.",
        };
    }
}
