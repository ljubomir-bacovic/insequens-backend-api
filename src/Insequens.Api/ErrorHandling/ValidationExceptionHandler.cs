using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.ErrorHandling;

/// <summary>FluentValidation failures as 400 with the messages grouped by property under <c>errors</c>.</summary>
public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ValidationExceptionHandler> logger)
    : ExceptionProblemHandler<ValidationException>(problemDetailsService)
{
    private const string ModelLevelErrorKey = "";

    protected override ProblemDetails CreateProblem(ValidationException exception)
    {
        var failures = exception.Errors.ToArray();
        var errors = failures
            .GroupBy(failure => failure.PropertyName ?? ModelLevelErrorKey)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        logger.LogWarning("Validation failed. Errors: {@ValidationErrors}", errors);

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = ProblemTypes.Validation,
            Title = "Validation failed.",
            Detail = string.Join("; ", failures.Select(failure => failure.ErrorMessage)),
        };
    }
}
