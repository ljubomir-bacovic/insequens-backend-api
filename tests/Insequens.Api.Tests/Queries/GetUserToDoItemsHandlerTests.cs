using AutoMapper;
using FluentAssertions;
using Insequens.Application;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.Data;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;
using Insequens.Infrastructure.Data.Models;
using Insequens.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Insequens.Api.Tests.Queries;

public class GetUserToDoItemsHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserHasNoMatchingItems_ReturnsEmptyPage()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        using var dataContext = new DataContext(context);
        var handler = new GetUserToDoItemsHandler(dataContext, CreateMapper());

        var result = await handler.Handle(new GetUserToDoItemsQuery(userId, false, 1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenResultsFitOnSinglePage_ReturnsAllMatchingItems()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        await SeedItemsAsync(context, userId);
        using var dataContext = new DataContext(context);
        var handler = new GetUserToDoItemsHandler(dataContext, CreateMapper());

        var result = await handler.Handle(new GetUserToDoItemsQuery(userId, false, 1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
        result.Items.Select(item => item.Name).Should().Equal("Task 1", "Task 2", "Task 3", "Task 4", "Task 5");
    }

    [Fact]
    public async Task Handle_ReturnsPaginatedItemsAndMetadata()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        await SeedItemsAsync(context, userId);
        using var dataContext = new DataContext(context);
        var handler = new GetUserToDoItemsHandler(dataContext, CreateMapper());

        var result = await handler.Handle(new GetUserToDoItemsQuery(userId, false, 2, 2), CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeTrue();
        result.Items.Select(item => item.Name).Should().Equal("Task 3", "Task 4");
    }

    [Fact]
    public async Task Handle_FiltersByCompletionStatusAndUser()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        await SeedItemsAsync(context, userId);
        using var dataContext = new DataContext(context);
        var handler = new GetUserToDoItemsHandler(dataContext, CreateMapper());

        var result = await handler.Handle(new GetUserToDoItemsQuery(userId, true, 1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Completed task");
    }

    [Fact]
    public async Task Handle_OrdersItemsByDueDateThenPriority()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        context.ToDoItems.AddRange(
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Medium priority", Priority = TaskPriority.Medium, DueDate = new DateOnly(2026, 2, 2), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Earlier due date", Priority = TaskPriority.Low, DueDate = new DateOnly(2026, 2, 1), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "High priority", Priority = TaskPriority.High, DueDate = new DateOnly(2026, 2, 2), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Low priority", Priority = TaskPriority.Low, DueDate = new DateOnly(2026, 2, 2), IsCompleted = false });

        await context.SaveChangesAsync();

        using var dataContext = new DataContext(context);
        var handler = new GetUserToDoItemsHandler(dataContext, CreateMapper());

        var result = await handler.Handle(new GetUserToDoItemsQuery(userId, false, 1, 10), CancellationToken.None);

        result.Items.Select(item => item.Name).Should().Equal(
            "Earlier due date",
            "High priority",
            "Medium priority",
            "Low priority");
    }

    [Fact]
    public async Task Handle_WhenRequestedPageIsBeyondAvailableRange_ReturnsEmptyItems()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        await SeedItemsAsync(context, userId);
        using var dataContext = new DataContext(context);
        var handler = new GetUserToDoItemsHandler(dataContext, CreateMapper());

        var result = await handler.Handle(new GetUserToDoItemsQuery(userId, false, 4, 2), CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeTrue();
        result.Items.Should().BeEmpty();
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static TestInsequensContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InsequensContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestInsequensContext(options);
    }

    private static async Task SeedItemsAsync(InsequensContext context, Guid userId)
    {
        var otherUserId = Guid.NewGuid();

        context.ToDoItems.AddRange(
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Task 1", Priority = TaskPriority.High, DueDate = new DateOnly(2026, 1, 1), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Task 2", Priority = TaskPriority.Low, DueDate = new DateOnly(2026, 1, 2), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Task 3", Priority = TaskPriority.Medium, DueDate = new DateOnly(2026, 1, 3), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Task 4", Priority = TaskPriority.Low, DueDate = new DateOnly(2026, 1, 4), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Task 5", Priority = TaskPriority.High, DueDate = new DateOnly(2026, 1, 5), IsCompleted = false },
            new ToDoItem { Id = Guid.NewGuid(), UserId = userId, Name = "Completed task", Priority = TaskPriority.Low, DueDate = new DateOnly(2026, 1, 6), IsCompleted = true },
            new ToDoItem { Id = Guid.NewGuid(), UserId = otherUserId, Name = "Other user's task", Priority = TaskPriority.Low, DueDate = new DateOnly(2026, 1, 1), IsCompleted = false });

        await context.SaveChangesAsync();
    }

    private sealed class TestInsequensContext : InsequensContext
    {
        [SetsRequiredMembers]
        public TestInsequensContext(DbContextOptions<InsequensContext> options) : base(options)
        {
            ToDoItems = Set<ToDoItem>();
        }
    }
}
