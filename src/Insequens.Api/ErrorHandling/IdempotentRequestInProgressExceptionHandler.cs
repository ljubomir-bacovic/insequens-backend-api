using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class IdempotentRequestInProgressExceptionHandler(ILogger<IdempotentRequestInProgressExceptionHandler> logger)
    : ExceptionProblemHandler<IdempotentRequestInProgressException>
{
    protected override ProblemDetails CreateProblem(IdempotentRequestInProgressException exception)
    {
        logger.LogInformation("Idempotent request retried while the first is in progress");

        return new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Type = ProblemTypes.IdempotentRequestInProgress,
            Title = "Request in progress.",
            Detail = exception.Message + " Retry later with the same key.",
        };
    }
}
