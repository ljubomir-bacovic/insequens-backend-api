using Asp.Versioning;
using Insequens.Api.Http;
using Insequens.Api.RateLimiting;
using Insequens.Api.Versioning;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Contracts.V1;
using Insequens.Contracts.V1.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Insequens.Api.Controllers;

/// <summary>The task endpoints at <c>/v1/ToDoItem</c>. Frozen: new behaviour goes to <see cref="TasksController"/>.</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiVersion(ApiVersions.V1)]
[Route(Constants.BaseUrl)]
[ApiController]
public class ToDoItemController(IMediator mediator) : TasksControllerBase(mediator)
{
    [HttpGet]
    [ProducesResponseType<PaginatedResult<ToDoItemGetListModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserToDoItemsAsync([FromQuery] bool isCompleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await Mediator.Send(new GetUserToDoItemsQuery(userId, isCompleted, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType<ToDoItemGetDetailsModel>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddToDoItemAsync([FromBody] ToDoItemCreateModel toDoItemCreate, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var item = await Mediator.Send(new CreateToDoItemCommand(
            toDoItemCreate.Name,
            toDoItemCreate.Description,
            toDoItemCreate.Priority,
            toDoItemCreate.DueDate,
            userId), cancellationToken);
        return CreatedAtAction(nameof(GetToDoItem), new { id = item.Id }, item);
    }

    [HttpPatch("{id:guid}/priority")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateToDoItemPriorityAsync(Guid id, [FromBody] TaskPriority priority, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(new UpdateToDoItemPriorityCommand(id, userId, priority, expectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/name")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateToDoItemNameAsync(Guid id, [FromBody] string name, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(new UpdateToDoItemNameCommand(id, userId, name, expectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/description")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateToDoItemDescriptionAsync(Guid id, [FromBody] string? description, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(new UpdateToDoItemDescriptionCommand(id, userId, description, expectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/duedate")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateToDoItemDueDateAsync(Guid id, [FromBody] DateOnly? date, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(new UpdateToDoItemDueDateCommand(id, userId, date, expectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> DeleteToDoItemAsync(Guid id, CancellationToken cancellationToken)
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ToDoItemGetDetailsModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetToDoItem(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var toDoItem = await Mediator.Send(new GetToDoItemQuery(id, userId), cancellationToken);
        Response.Headers.ETag = EntityTags.Format(toDoItem.Version);
        return Ok(toDoItem.Value);
    }

    [HttpPatch("{id:guid}/togglecomplete")]
    [Obsolete("A retried toggle flips the task back. Use PUT /v2/Tasks/{id}/completion.")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> CompleteToDoItem(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!EntityTags.TryParseIfMatch(Request, out var expectedVersion))
        {
            return UnmatchableIfMatch();
        }

        await Mediator.Send(new ToggleToDoItemCompleteCommand(id, userId, expectedVersion), cancellationToken);
        return NoContent();
    }
}
