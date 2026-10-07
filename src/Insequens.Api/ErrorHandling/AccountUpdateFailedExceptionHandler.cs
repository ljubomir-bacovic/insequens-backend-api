using Insequens.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public sealed class AccountUpdateFailedExceptionHandler(ILogger<AccountUpdateFailedExceptionHandler> logger)
    : ExceptionProblemHandler<AccountUpdateFailedException>
{
    protected override ProblemDetails CreateProblem(AccountUpdateFailedException exception)
    {
        logger.LogInformation("Account update refused: {Reason}", exception.Message);

        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = ProblemTypes.AccountUpdateFailed,
            Title = "Account update failed.",
            Detail = exception.Message,
        };
    }
}
