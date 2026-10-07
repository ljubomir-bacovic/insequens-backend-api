using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

public static class ProblemResponses
{
    public const string ContentType = "application/problem+json";

    /// <summary>
    /// Writes <paramref name="problem"/> through <see cref="IProblemDetailsService"/>. That service writes nothing
    /// when the client's <c>Accept</c> excludes JSON (for example <c>text/plain</c>), and an error must still get
    /// its status and body, so this falls back to writing <c>application/problem+json</c> itself.
    /// </summary>
    public static async Task WriteAsync(
        HttpContext httpContext,
        ProblemDetails problem,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        };

        if (await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(context))
        {
            return;
        }

        ProblemDetailsServiceCollectionExtensions.Customize(context);
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, ContentType, cancellationToken);
    }
}
