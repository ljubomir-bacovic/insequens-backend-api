using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class IdempotencyKeyReusedExceptionHandler(ILogger<IdempotencyKeyReusedExceptionHandler> logger)
    : ExceptionProblemHandler<IdempotencyKeyReusedException>
{
    protected override ProblemDetails CreateProblem(IdempotencyKeyReusedException exception)
    {
        logger.LogWarning("Idempotency-Key reused with a different request");

        return new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Type = ProblemTypes.IdempotencyKeyReused,
            Title = "Idempotency-Key reused.",
            Detail = exception.Message + " Send a new key for a new request.",
        };
    }
}
