using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

/// <summary>
/// Turns one exception type into a ProblemDetails response. <see cref="IProblemDetailsService"/> writes it, so
/// every error carries the same <c>traceId</c> and <c>instance</c> as framework-generated problems.
/// </summary>
public abstract class ExceptionProblemHandler<TException>(IProblemDetailsService problemDetailsService) : IExceptionHandler
    where TException : Exception
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not TException typedException)
        {
            return false;
        }

        var problem = CreateProblem(typedException);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    /// <summary>Logs the failure and describes it; the status, type and title are required.</summary>
    protected abstract ProblemDetails CreateProblem(TException exception);
}
