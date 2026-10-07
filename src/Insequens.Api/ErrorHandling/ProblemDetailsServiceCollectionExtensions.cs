using System.Diagnostics;

namespace Insequens.Api.ErrorHandling;

public static class ProblemDetailsServiceCollectionExtensions
{
    public const string TraceIdKey = "traceId";

    /// <summary>
    /// ProblemDetails for every error: exceptions through the handlers below, framework status codes (401 from
    /// the JWT handler, 404 for an unknown route) through <c>UseStatusCodePages</c>, and MVC's own 400s.
    /// </summary>
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions[TraceIdKey] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        });

        // Handlers run in registration order and the first that handles the exception wins.
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<ForbiddenExceptionHandler>();
        services.AddExceptionHandler<PreconditionFailedExceptionHandler>();
        services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>();
        services.AddExceptionHandler<AuthenticationFailedExceptionHandler>();
        services.AddExceptionHandler<EmailConfirmationFailedExceptionHandler>();
        services.AddExceptionHandler<AccountUpdateFailedExceptionHandler>();
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<UnhandledExceptionHandler>();

        return services;
    }
}
