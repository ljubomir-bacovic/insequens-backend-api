using Insequens.Api.RateLimiting;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Queries.ToDoItem;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Insequens.Contracts.V1.Tasks;
using Insequens.Contracts.V1;

namespace Insequens.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route(Constants.BaseUrl)]
[ApiController]
public class ToDoItemController : ControllerBase
{
    private readonly IMediator _mediator;

    public ToDoItemController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResult<ToDoItemGetListModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserToDoItemsAsync([FromQuery] bool isCompleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetUserToDoItemsQuery(userId, isCompleted, page, pageSize), cancellationToken);
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

        var item = await _mediator.Send(new CreateToDoItemCommand(
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
    public async Task<IActionResult> UpdateToDoItemPriorityAsync(Guid id, [FromBody] TaskPriority priority, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _mediator.Send(new UpdateToDoItemPriorityCommand(id, userId, priority), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/name")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToDoItemNameAsync(Guid id, [FromBody] string name, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _mediator.Send(new UpdateToDoItemNameCommand(id, userId, name), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/description")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToDoItemDescriptionAsync(Guid id, [FromBody] string? description, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _mediator.Send(new UpdateToDoItemDescriptionCommand(id, userId, description), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/duedate")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToDoItemDueDateAsync(Guid id, [FromBody] DateOnly? date, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _mediator.Send(new UpdateToDoItemDueDateCommand(id, userId, date), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteToDoItemAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _mediator.Send(new DeleteToDoItemCommand(id, userId), cancellationToken);
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

        var toDoItem = await _mediator.Send(new GetToDoItemQuery(id, userId), cancellationToken);
        return Ok(toDoItem);
    }

    [HttpPatch("{id:guid}/togglecomplete")]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteToDoItem(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        await _mediator.Send(new ToggleToDoItemCompleteCommand(id, userId), cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }
}
