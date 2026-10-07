using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

/// <summary>
/// Turns one exception type into a ProblemDetails response, written by <see cref="ProblemResponses"/> so every
/// error carries the same <c>traceId</c> and <c>instance</c> as framework-generated problems.
/// </summary>
public abstract class ExceptionProblemHandler<TException> : IExceptionHandler
    where TException : Exception
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not TException typedException)
        {
            return false;
        }

        await ProblemResponses.WriteAsync(httpContext, CreateProblem(typedException), exception, cancellationToken);

        return true;
    }

    /// <summary>Logs the failure and describes it; the status, type and title are required.</summary>
    protected abstract ProblemDetails CreateProblem(TException exception);
}
