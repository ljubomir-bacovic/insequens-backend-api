using FluentAssertions;
using Insequens.Api.Controllers;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Models;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

namespace Insequens.Api.Tests.Controllers;

public class ToDoItemControllerTests
{
    [Fact]
    public void Constructor_WhenInspected_DependsOnlyOnMediator()
    {
        var constructor = typeof(ToDoItemController).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Should().ContainSingle()
            .Which.ParameterType.Should().Be(typeof(IMediator));
    }

    [Fact]
    public void Controller_WhenInspected_HasExpectedClassLevelRoutingMetadata()
    {
        var controllerType = typeof(ToDoItemController);
        var apiControllerAttribute = controllerType.GetCustomAttribute<ApiControllerAttribute>();
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        apiControllerAttribute.Should().NotBeNull();
        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be(Constants.BaseUrl);
        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.AuthenticationSchemes.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Theory]
    [MemberData(nameof(ActionRouteMetadata))]
    public void Actions_WhenInspected_HaveExpectedRouteMetadata(
        string methodName,
        Type expectedAttributeType,
        string? expectedTemplate)
    {
        var method = typeof(ToDoItemController).GetMethod(methodName);

        method.Should().NotBeNull();
        method!.GetCustomAttributes(expectedAttributeType, inherit: true).Should().ContainSingle();

        if (method.GetCustomAttributes(expectedAttributeType, inherit: true).Single() is HttpMethodAttribute attribute)
        {
            attribute.Template.Should().Be(expectedTemplate);
        }
    }

    [Fact]
    public async Task GetUserToDoItemsAsync_WhenCalled_ReturnsPaginatedResult()
    {
        var userId = Guid.NewGuid();
        var expected = new PaginatedResult<ToDoItemGetListModel>(
            [new ToDoItemGetListModel(Guid.NewGuid(), "Task 1", "Description", new DateOnly(2026, 1, 1), false, TaskPriority.High)],
            1,
            1,
            20);
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Is<GetUserToDoItemsQuery>(q => q.UserId == userId && !q.IsCompleted && q.Page == 1 && q.PageSize == 20), Arg.Any<CancellationToken>())
            .Returns(expected);
        var controller = CreateController(userId, mediator);

        var actionResult = await controller.GetUserToDoItemsAsync(isCompleted: false, page: 1, pageSize: 20);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        await mediator.Received(1)
            .Send(Arg.Is<GetUserToDoItemsQuery>(q => q.UserId == userId && !q.IsCompleted && q.Page == 1 && q.PageSize == 20), Arg.Any<CancellationToken>());

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
    public async Task AddToDoItemAsync_WhenCalled_ReturnsCreatedAtActionAndSendsCreateCommand()
    {
        var userId = Guid.NewGuid();
        var createdItem = new ToDoItemGetDetailsModel(Guid.NewGuid(), "Task 1", "Description", TaskPriority.High, new DateOnly(2026, 12, 31), false);
        var request = new ToDoItemCreateModel("Task 1", "Description", (int)TaskPriority.High, new DateOnly(2026, 12, 31));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(
                Arg.Is<CreateToDoItemCommand>(command =>
                    command.Name == request.Name &&
                    command.Description == request.Description &&
                    command.Priority == request.Priority &&
                    command.DueDate == request.DueDate &&
                    command.UserId == userId),
                Arg.Any<CancellationToken>())
            .Returns(createdItem);
        var controller = CreateController(userId, mediator);

        var result = await controller.AddToDoItemAsync(request);

        var createdAtActionResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtActionResult.ActionName.Should().Be(nameof(ToDoItemController.GetToDoItem));
        createdAtActionResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(createdItem.Id);
        createdAtActionResult.Value.Should().BeSameAs(createdItem);
        await mediator.Received(1)
            .Send(
                Arg.Is<CreateToDoItemCommand>(command =>
                    command.Name == request.Name &&
                    command.Description == request.Description &&
                    command.Priority == request.Priority &&
                    command.DueDate == request.DueDate &&
                    command.UserId == userId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateToDoItemPriorityAsync_WhenCalled_SendsUpdatePriorityCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var priority = TaskPriority.High;
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateToDoItemPriorityCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.UpdateToDoItemPriorityAsync(itemId, priority);

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1)
            .Send(Arg.Is<UpdateToDoItemPriorityCommand>(command => command.ItemId == itemId && command.UserId == userId && command.Priority == priority), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateToDoItemNameAsync_WhenCalled_SendsUpdateNameCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string name = "Updated task name";
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateToDoItemNameCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.UpdateToDoItemNameAsync(itemId, name);

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1)
            .Send(Arg.Is<UpdateToDoItemNameCommand>(command => command.ItemId == itemId && command.UserId == userId && command.Name == name), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateToDoItemDescriptionAsync_WhenCalled_SendsUpdateDescriptionCommandAndCancellationToken()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        const string description = "Updated task description";
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateToDoItemDescriptionCommand>(), cancellationToken).Returns(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.UpdateToDoItemDescriptionAsync(itemId, description, cancellationToken);

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1)
            .Send(Arg.Is<UpdateToDoItemDescriptionCommand>(command => command.ItemId == itemId && command.UserId == userId && command.Description == description), cancellationToken);
    }

    [Fact]
    public async Task UpdateToDoItemDueDateAsync_WhenCalled_SendsUpdateDueDateCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var dueDate = new DateOnly(2026, 12, 31);
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<UpdateToDoItemDueDateCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.UpdateToDoItemDueDateAsync(itemId, dueDate);

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1)
            .Send(Arg.Is<UpdateToDoItemDueDateCommand>(command => command.ItemId == itemId && command.UserId == userId && command.DueDate == dueDate), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteToDoItemAsync_WhenCalled_SendsDeleteCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DeleteToDoItemCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.DeleteToDoItemAsync(itemId);

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1)
            .Send(Arg.Is<DeleteToDoItemCommand>(command => command.ItemId == itemId && command.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetToDoItem_WhenCalled_SendsQueryAndCancellationToken()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var expected = new ToDoItemGetDetailsModel(
            itemId,
            "Task 1",
            "Description",
            TaskPriority.High,
            new DateOnly(2026, 12, 31),
            false);
        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Is<GetToDoItemQuery>(q => q.ItemId == itemId && q.UserId == userId), cancellationToken)
            .Returns(expected);
        var controller = CreateController(userId, mediator);

        var result = await controller.GetToDoItem(itemId, cancellationToken);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(expected);
        await mediator.Received(1)
            .Send(Arg.Is<GetToDoItemQuery>(q => q.ItemId == itemId && q.UserId == userId), cancellationToken);
    }

    [Fact]
    public async Task CompleteToDoItem_WhenCalled_SendsToggleCommand()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ToggleToDoItemCompleteCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);
        var controller = CreateController(userId, mediator);

        var result = await controller.CompleteToDoItem(itemId);

        result.Should().BeOfType<OkResult>();
        await mediator.Received(1)
            .Send(Arg.Is<ToggleToDoItemCompleteCommand>(command => command.ItemId == itemId && command.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetToDoItem_WhenMediatorThrows_PropagatesException()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetToDoItemQuery>(), Arg.Any<CancellationToken>()).Returns<Task<ToDoItemGetDetailsModel>>(_ => throw new InvalidOperationException("boom"));
        var controller = CreateController(Guid.NewGuid(), mediator);

        var act = () => controller.GetToDoItem(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    public static TheoryData<string, Type, string?> ActionRouteMetadata =>
        new()
        {
            { nameof(ToDoItemController.GetUserToDoItemsAsync), typeof(HttpGetAttribute), null },
            { nameof(ToDoItemController.AddToDoItemAsync), typeof(HttpPostAttribute), null },
            { nameof(ToDoItemController.UpdateToDoItemPriorityAsync), typeof(HttpPatchAttribute), "{id:guid}/priority" },
            { nameof(ToDoItemController.UpdateToDoItemNameAsync), typeof(HttpPatchAttribute), "{id:guid}/name" },
            { nameof(ToDoItemController.UpdateToDoItemDescriptionAsync), typeof(HttpPatchAttribute), "{id:guid}/description" },
            { nameof(ToDoItemController.UpdateToDoItemDueDateAsync), typeof(HttpPatchAttribute), "{id:guid}/duedate" },
            { nameof(ToDoItemController.DeleteToDoItemAsync), typeof(HttpDeleteAttribute), "{id:guid}" },
            { nameof(ToDoItemController.GetToDoItem), typeof(HttpGetAttribute), "{id:guid}" },
            { nameof(ToDoItemController.CompleteToDoItem), typeof(HttpPatchAttribute), "{id:guid}/togglecomplete" },
        };

    private static ToDoItemController CreateController(Guid userId, IMediator mediator)
    {
        var controller = new ToDoItemController(mediator);
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
}
