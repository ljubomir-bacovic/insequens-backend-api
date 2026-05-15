using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Models;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Insequens.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route(Constants.BaseUrl)]
[ApiController]
public class ToDoItemController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid UserId => Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

    public ToDoItemController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResult<ToDoItemGetListModel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserToDoItemsAsync([FromQuery] bool isCompleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUserToDoItemsQuery(UserId, isCompleted, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<ToDoItemGetDetailsModel>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddToDoItemAsync([FromBody] ToDoItemCreateModel toDoItemCreate, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new CreateToDoItemCommand(
            toDoItemCreate.Name,
            toDoItemCreate.Description,
            toDoItemCreate.Priority,
            toDoItemCreate.DueDate,
            UserId), cancellationToken);
        return CreatedAtAction(nameof(GetToDoItem), new { id = item.Id }, item);
    }

    [HttpPatch("{id:guid}/priority")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToDoItemPriorityAsync(Guid id, [FromBody] TaskPriority priority, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateToDoItemPriorityCommand(id, UserId, priority), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/name")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToDoItemNameAsync(Guid id, [FromBody] string name, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateToDoItemNameCommand(id, UserId, name), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/description")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToDoItemDescriptionAsync(Guid id, [FromBody] string? description, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateToDoItemDescriptionCommand(id, UserId, description), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/duedate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateToDoItemDueDateAsync(Guid id, [FromBody] DateOnly date, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateToDoItemDueDateCommand(id, UserId, date), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteToDoItemAsync(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteToDoItemCommand(id, UserId), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ToDoItemGetDetailsModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetToDoItem(Guid id, CancellationToken cancellationToken)
    {
        var toDoItem = await _mediator.Send(new GetToDoItemQuery(id, UserId), cancellationToken);
        return Ok(toDoItem);
    }

    [HttpPatch("{id:guid}/togglecomplete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteToDoItem(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ToggleToDoItemCompleteCommand(id, UserId), cancellationToken);
        return Ok();
    }
}
