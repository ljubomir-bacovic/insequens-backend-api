using Insequens.Application.Models;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.ServiceContracts;
using Insequens.Domain.Types;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace Insequens.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(Constants.BaseUrl)]
    [ApiController]
    public class ToDoItemController : ControllerBase
    {
        private readonly ISender _sender;
        IToDoItemService _toDoItemService;
        Guid UserId => Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value);

        public ToDoItemController(IToDoItemService toDoItemService, ISender sender)
        {
            _toDoItemService = toDoItemService;
            _sender = sender;
        }

        /*
        [HttpGet]
        [ProducesResponseType<List<ToDoItemGetListModel>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> GetUserToDoList()
        {
            var toDoList = await _toDoItemService.GetUserToDoItemsAsync(UserId);
            return Results.Ok(toDoList);
        }
        */

        [HttpGet]
        [ProducesResponseType<PaginatedResult<ToDoItemGetListModel>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUserToDoItemsAsync([FromQuery] bool isCompleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Page and pageSize must be greater than 0.");
            }

            var result = await _sender.Send(new GetUserToDoItemsQuery(UserId, isCompleted, page, pageSize));
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IResult> AddToDoItemAsync(ToDoItemCreateModel toDoItemCreate)
        {
            var item = await _toDoItemService.AddToDoItemAsync(toDoItemCreate, UserId);
            var location = Url.Action(nameof(AddToDoItemAsync), new { id = item.Id }) ?? $"api/ToDoItem/{item.Id}";
            return Results.Created(location, item);
        }

        [HttpPatch("{id:guid}/priority")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> UpdateToDoItemPriorityAsync(Guid id, [FromBody] TaskPriority priority)
        {
            await _toDoItemService.UpdateToDoItemPriorityAsync(id, UserId, priority);
            return Results.NoContent();
        }

        [HttpPatch("{id:guid}/name")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> UpdateToDoItemNameAsync(Guid id, [FromBody] string name)
        {
            await _toDoItemService.UpdateToDoItemNameAsync(id, UserId, name);
            return Results.NoContent();
        }

        [HttpPatch("{id:guid}/description")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> UpdateToDoItemDescriptionAsync(Guid id, [FromBody] string description)
        {
            await _toDoItemService.UpdateToDoItemDescriptionAsync(id, UserId, description);
            return Results.NoContent();
        }

        [HttpPatch("{id:guid}/duedate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> UpdateToDoItemDueDateAsync(Guid id, [FromBody] DateOnly date)
        {
            await _toDoItemService.UpdateToDoItemDueDateAsync(id, UserId, date);
            return Results.NoContent();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> DeleteToDoItemAsync(Guid id)
        {
            await _toDoItemService.DeleteToDoItemAsync(id, UserId);
            return Results.NoContent();
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<ToDoItemGetDetailsModel>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> GetToDoItem(Guid id)
        {
            var toDoItem = await _toDoItemService.GetToDoItem(id, UserId);
            return Results.Ok(toDoItem);
        }

        [HttpPatch("{id:guid}/togglecomplete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IResult> CompleteToDoItem(Guid id)
        {
            await _toDoItemService.ToggleToDoItemCompleteAsync(id, UserId);
            return Results.Ok();
        }
    }
}
