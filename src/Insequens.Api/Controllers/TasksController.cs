using Asp.Versioning;
using Insequens.Api.Http;
using Insequens.Api.RateLimiting;
using Insequens.Api.Versioning;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Queries.Tasks;
using Insequens.Contracts.V1;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Insequens.Api.Controllers;

/// <summary>
/// The task endpoints at <c>/v2/Tasks</c>: string priorities with <c>none</c>, list filters and sorting, one
/// partial-update PATCH and an idempotent completion PUT in place of v1's per-field PATCHes and toggle.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiVersion(ApiVersions.V2)]
[Route(Constants.BaseUrl)]
[ApiController]
public class TasksController(IMediator mediator) : TasksControllerBase(mediator)
{
    [HttpGet]
    [ProducesResponseType<PaginatedResult<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] bool? completed,
        [FromQuery] TaskPriority? priority,
        [FromQuery] DateOnly? dueFrom,
        [FromQuery] DateOnly? dueTo,
        [FromQuery] string? search,
        [FromQuery] TaskSortField sortBy = TaskSortField.DueDate,
        [FromQuery] SortDirection? sortDirection = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await Mediator.Send(
            new ListTasksQuery(userId, completed, priority, dueFrom, dueTo, search, sortBy, sortDirection, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var task = await Mediator.Send(
            new CreateTaskCommand(userId, request.Name, request.Description, request.Priority, request.DueDate),
            cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = task.Id }, task);
    }

    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetAsync))]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var task = await Mediator.Send(new GetTaskQuery(id, userId), cancellationToken);
        Response.Headers.ETag = EntityTags.Format(task.Version);
        return Ok(task.Value);
    }

    [HttpPatch("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(
            new UpdateTaskCommand(id, userId, request.Name, request.Description, request.Priority, request.DueDate, expectedVersion),
            cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/completion")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> SetCompletionAsync(
        Guid id,
        [FromBody] SetTaskCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(new SetTaskCompletionCommand(id, userId, request.Completed, expectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(new DeleteToDoItemCommand(id, userId, expectedVersion), cancellationToken);
        return NoContent();
    }
}
