using Asp.Versioning;
using FluentValidation;
using FluentValidation.Results;
using Insequens.Application.Exceptions;
using Insequens.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Tests.ErrorHandling;

/// <summary>Throws the exception named in the route, so the exception handlers can be tested over HTTP.</summary>
[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[Route("test/throw")]
public class ThrowingTestController : ControllerBase
{
    public const string ResourceId = "6f1d2c0e-6b55-4a43-9d8e-0d6b7a1f0c11";

    [HttpGet("{kind}")]
    public IActionResult Throw(string kind) => throw kind switch
    {
        "not-found" => new NotFoundException("ToDoItem", Guid.Parse(ResourceId)),
        "forbidden" => new ForbiddenException("Admin"),
        "authentication" => new AuthenticationFailedException(),
        "email-confirmation" => new EmailConfirmationFailedException(),
        "account-update" => new AccountUpdateFailedException("The current password is incorrect."),
        "domain" => new ToDoItemDescriptionTooLongException(4000),
        "idempotency-key-reused" => new IdempotencyKeyReusedException(),
        "idempotent-request-in-progress" => new IdempotentRequestInProgressException(),
        "validation" => new ValidationException(
            "Do not leak this exception message.",
            [
                new ValidationFailure("Name", "Name is required."),
                new ValidationFailure("Name", "Name must be at least 3 characters."),
                new ValidationFailure("Priority", "Priority must be between 0 and 3."),
            ]),
        "validation-empty" => new ValidationException("Do not leak this exception message.", []),
        "validation-model" => new ValidationException(
            "Do not leak this exception message.",
            [new ValidationFailure("", "A general validation failure occurred.")]),
        _ => new InvalidOperationException("Secret failure detail.", new TimeoutException("Inner secret.")),
    };
}
