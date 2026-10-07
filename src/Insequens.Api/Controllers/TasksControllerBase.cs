using System.Security.Claims;
using Insequens.Api.ErrorHandling;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Insequens.Api.Controllers;

/// <summary>What the v1 (<see cref="ToDoItemController"/>) and v2 (<see cref="TasksController"/>) task endpoints share.</summary>
public abstract class TasksControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    /// <summary>An If-Match that names no version of ours (malformed, weak or several tags) can never match.</summary>
    protected ObjectResult UnmatchableIfMatch() => Problem(
        statusCode: StatusCodes.Status412PreconditionFailed,
        type: ProblemTypes.PreconditionFailed,
        title: PreconditionFailedExceptionHandler.Title,
        detail: "If-Match must be one ETag returned by this API, or *.");

    protected bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }
}
