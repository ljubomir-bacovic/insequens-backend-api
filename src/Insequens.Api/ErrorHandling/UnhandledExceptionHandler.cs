using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

/// <summary>The last handler: any other exception is a 500, with the message only in Development.</summary>
public sealed class UnhandledExceptionHandler(
    IHostEnvironment environment,
    ILogger<UnhandledExceptionHandler> logger)
    : ExceptionProblemHandler<Exception>
{
    protected override ProblemDetails CreateProblem(Exception exception)
    {
        logger.LogError(exception, "Unhandled exception.");

        var innerDetail = string.IsNullOrEmpty(exception.InnerException?.Message)
            ? string.Empty
            : " Inner Exception: " + exception.InnerException.Message;

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = ProblemTypes.Internal,
            Title = "Internal Server Error",
            Detail = environment.IsDevelopment()
                ? "Message: " + exception.Message + innerDetail
                : "An unexpected error occurred. Please try again later.",
        };
    }
}
