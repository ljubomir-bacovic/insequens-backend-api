using FluentAssertions;
using Insequens.Api.Controllers;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Models;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.ServiceContracts;
using Insequens.Domain.Types;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Insequens.Api.Tests.Controllers;

public class ToDoItemControllerTests
{
    [Fact]
    public async Task GetUserToDoItemsAsync_WhenPaginationIsValid_ReturnsPaginatedResult()
    {
        var userId = Guid.NewGuid();
        var expected = new PaginatedResult<ToDoItemGetListModel>(
            [new ToDoItemGetListModel(Guid.NewGuid(), "Task 1", "Description", new DateOnly(2026, 1, 1), false, TaskPriority.High)],
            1,
            1,
            20);
        var mediator = new TestMediator(expected);
        var controller = CreateController(userId, mediator);

        var actionResult = await controller.GetUserToDoItemsAsync(isCompleted: false, page: 1, pageSize: 20);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        mediator.LastRequest.Should().Be(new GetUserToDoItemsQuery(userId, false, 1, 20));

        var json = JsonSerializer.Serialize(okResult.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"totalCount\":1");
        json.Should().Contain("\"page\":1");
        json.Should().Contain("\"pageSize\":20");
        json.Should().Contain("\"totalPages\":1");
        json.Should().Contain("\"hasNext\":false");
        json.Should().Contain("\"hasPrevious\":false");
    }

    [Fact]
    public async Task CompleteToDoItem_WhenCalled_SendsToggleCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var mediator = new TestMediator(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.CompleteToDoItem(itemId);

        result.Should().BeOfType<OkResult>();
        mediator.LastRequest.Should().Be(new ToggleToDoItemCompleteCommand(itemId, userId));
    }

    [Fact]
    public async Task UpdateToDoItemPriorityAsync_WhenCalled_SendsUpdatePriorityCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var priority = TaskPriority.High;
        var mediator = new TestMediator(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.UpdateToDoItemPriorityAsync(itemId, priority);

        result.Should().BeOfType<NoContentResult>();
        mediator.LastRequest.Should().Be(new UpdateToDoItemPriorityCommand(itemId, userId, priority));
    }

    [Fact]
    public async Task UpdateToDoItemNameAsync_WhenCalled_SendsUpdateNameCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string name = "Updated task name";
        var mediator = new TestMediator(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.UpdateToDoItemNameAsync(itemId, name);

        result.Should().BeOfType<NoContentResult>();
        mediator.LastRequest.Should().Be(new UpdateToDoItemNameCommand(itemId, userId, name));
    }

    [Fact]
    public async Task UpdateToDoItemDescriptionAsync_WhenCalled_SendsUpdateDescriptionCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string description = "Updated task description";
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mediator = new TestMediator(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.UpdateToDoItemDescriptionAsync(itemId, description, cancellationToken);

        result.Should().BeOfType<NoContentResult>();
        mediator.LastRequest.Should().Be(new UpdateToDoItemDescriptionCommand(itemId, userId, description));
        mediator.LastCancellationToken.Should().Be(cancellationToken);
    }

    private static ToDoItemController CreateController(Guid userId, IMediator mediator)
    {
        var controller = new ToDoItemController(new StubToDoItemService(), mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    "TestAuthType")),
            },
        };

        return controller;
    }

    private sealed class TestMediator(object response) : IMediator
    {
        public object? LastRequest { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            return Task.FromResult<object?>(response);
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubToDoItemService : IToDoItemService
    {
        public Task<ToDoItemGetDetailsModel> AddToDoItemAsync(ToDoItemCreateModel toDoItemCreate, Guid userId) => throw new NotSupportedException();
        public Task DeleteToDoItemAsync(Guid id, Guid userId) => throw new NotSupportedException();
        public Task<ToDoItemGetDetailsModel> GetToDoItem(Guid id, Guid userId) => throw new NotSupportedException();
        public Task<List<ToDoItemGetListModel>> GetUserToDoItemsAsync(Guid userId, bool isCompleted, int page, int pageSize) => throw new NotSupportedException();
        public Task ToggleToDoItemCompleteAsync(Guid id, Guid userId) => throw new NotSupportedException();
        public Task UpdateToDoItemAsync(ToDoItemUpdateModel toDoItemUpdate, Guid userId) => throw new NotSupportedException();
        public Task UpdateToDoItemDescriptionAsync(Guid id, Guid userId, string description) => throw new NotSupportedException();
        public Task UpdateToDoItemDueDateAsync(Guid id, Guid userId, DateOnly date) => throw new NotSupportedException();
        public Task UpdateToDoItemNameAsync(Guid id, Guid userId, string name) => throw new NotSupportedException();
        public Task UpdateToDoItemPriorityAsync(Guid id, Guid userId, TaskPriority priority) => throw new NotSupportedException();
    }
}
