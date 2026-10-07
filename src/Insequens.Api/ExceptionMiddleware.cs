using Insequens.Application.Exceptions;
using Insequens.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Insequens.Api;

public class ExceptionMiddleware
{
    private const string ModelLevelErrorKey = "";
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private RequestDelegate Next { get; }

    public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<ExceptionMiddleware> logger)
    {
        Next = next;
        _env = env;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (ToDoItemNotFoundException ex)
        {
            _logger.LogWarning("To Do item not found. Id: {ItemId}", ex.Id);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status404NotFound,
                Detail = string.Empty,
                Instance = "",
                Title = $"To Do item for id {ex.Id} not found.",
                Type = "Error"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
        catch (ResourceForbiddenException ex)
        {
            _logger.LogWarning("Forbidden access attempt. ResourceId: {ResourceId}", ex.Id);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status403Forbidden,
                Detail = string.Empty,
                Instance = "",
                Title = "Access denied.",
                Type = "Error"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
        catch (AuthenticationFailedException)
        {
            // One body for every reason, so a client cannot tell an unknown email from a wrong password,
            // an unconfirmed account or a lockout.
            _logger.LogInformation("Authentication failed.");

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status401Unauthorized,
                Detail = string.Empty,
                Instance = "",
                Title = "Authentication failed.",
                Type = "Error"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
        catch (EmailConfirmationFailedException)
        {
            _logger.LogInformation("Email confirmation failed.");

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Empty,
                Instance = "",
                Title = "Email confirmation failed.",
                Type = "Error"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain rule violated. Rule: {DomainRule}", ex.GetType().Name);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Instance = "",
                Title = "Domain rule violated.",
                Type = "Error"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error. Details: {ValidationDetails}", ex.Value);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = JsonSerializer.Serialize(ex.Value),
                Instance = "",
                Title = "Validation Error",
                Type = "Error"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var failures = ex.Errors.ToArray();
            var errors = failures
                .GroupBy(failure => failure.PropertyName ?? ModelLevelErrorKey)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

            _logger.LogWarning("Validation failed. Errors: {@ValidationErrors}", errors);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", failures.Select(failure => failure.ErrorMessage)),
                Title = "Validation failed.",
                Type = "ValidationError",
            };

            problemDetails.Extensions["errors"] = errors;

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var innerExceptionMessage = ex.InnerException?.Message;
            var innerExceptionDetail = string.IsNullOrEmpty(innerExceptionMessage)
                ? string.Empty
                : " Inner Exception: " + innerExceptionMessage;

            var detail = _env.IsDevelopment()
                ? "Message: " + ex.Message + innerExceptionDetail
                : "An unexpected error occurred. Please try again later.";

            var problemDetails = new ProblemDetails()
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = detail,
                Instance = "",
                Title = "Internal Server Error",
                Type = "Error"
            };

            var problemDetailsJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(problemDetailsJson);
        }
    }
}
