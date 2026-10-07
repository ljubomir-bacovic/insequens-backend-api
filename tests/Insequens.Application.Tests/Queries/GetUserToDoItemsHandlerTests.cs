using FluentAssertions;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Application.Tests.Support;
using Insequens.Domain.Types;

namespace Insequens.Application.Tests.Queries;

public sealed class GetUserToDoItemsHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WhenUserHasNoMatchingItems_ReturnsEmptyPage()
    {
        var result = await _database.SendAsync(new GetUserToDoItemsQuery(Guid.NewGuid(), false, 1, 10));

        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_WhenResultsFitOnSinglePage_ReturnsAllMatchingItems()
    {
        var userId = Guid.NewGuid();
        await SeedItemsAsync(userId);

        var result = await _database.SendAsync(new GetUserToDoItemsQuery(userId, false, 1, 10));

        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(1);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
        result.Items.Select(item => item.Name).Should().Equal("Task 1", "Task 2", "Task 3", "Task 4", "Task 5");
    }

    [Fact]
    public async Task Send_WithSecondPage_ReturnsPageItemsAndMetadata()
    {
        var userId = Guid.NewGuid();
        await SeedItemsAsync(userId);

        var result = await _database.SendAsync(new GetUserToDoItemsQuery(userId, false, 2, 2));

        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeTrue();
        result.Items.Select(item => item.Name).Should().Equal("Task 3", "Task 4");
    }

    [Fact]
    public async Task Send_WithCompletedFilter_ReturnsOnlyTheUsersCompletedItems()
    {
        var userId = Guid.NewGuid();
        await SeedItemsAsync(userId);

        var result = await _database.SendAsync(new GetUserToDoItemsQuery(userId, true, 1, 10));

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Name.Should().Be("Completed task");
    }

    [Fact]
    public async Task Send_WithItemsOnTheSameDueDate_OrdersByDueDateThenPriority()
    {
        var userId = Guid.NewGuid();
        await _database.SeedItemAsync(userId, "Medium priority", priority: TaskPriority.Medium, dueDate: new DateOnly(2026, 2, 2));
        await _database.SeedItemAsync(userId, "Earlier due date", priority: TaskPriority.Low, dueDate: new DateOnly(2026, 2, 1));
        await _database.SeedItemAsync(userId, "High priority", priority: TaskPriority.High, dueDate: new DateOnly(2026, 2, 2));
        await _database.SeedItemAsync(userId, "Low priority", priority: TaskPriority.Low, dueDate: new DateOnly(2026, 2, 2));

        var result = await _database.SendAsync(new GetUserToDoItemsQuery(userId, false, 1, 10));

        result.Items.Select(item => item.Name).Should().Equal(
            "Earlier due date",
            "High priority",
            "Medium priority",
            "Low priority");
    }

    [Fact]
    public async Task Send_WithItemWithoutDueDate_ReturnsNullDueDate()
    {
        var userId = Guid.NewGuid();
        await _database.SeedItemAsync(userId, "No due date");

        var result = await _database.SendAsync(new GetUserToDoItemsQuery(userId, false, 1, 10));

        result.Items.Should().ContainSingle().Which.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Send_WithItemsWithAndWithoutDueDate_PutsThoseWithoutLast()
    {
        var userId = Guid.NewGuid();
        await _database.SeedItemAsync(userId, "No due date", priority: TaskPriority.High);
        await _database.SeedItemAsync(userId, "Due", dueDate: new DateOnly(2026, 2, 1));

        var result = await _database.SendAsync(new GetUserToDoItemsQuery(userId, false, 1, 10));

        result.Items.Select(item => item.Name).Should().Equal("Due", "No due date");
    }

    [Fact]
    public async Task Send_WhenRequestedPageIsBeyondAvailableRange_ReturnsEmptyItems()
    {
        var userId = Guid.NewGuid();
        await SeedItemsAsync(userId);

        var result = await _database.SendAsync(new GetUserToDoItemsQuery(userId, false, 4, 2));

        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeTrue();
        result.Items.Should().BeEmpty();
    }

    private async Task SeedItemsAsync(Guid userId)
    {
        await _database.SeedItemAsync(userId, "Task 1", priority: TaskPriority.High, dueDate: new DateOnly(2026, 1, 1));
        await _database.SeedItemAsync(userId, "Task 2", priority: TaskPriority.Low, dueDate: new DateOnly(2026, 1, 2));
        await _database.SeedItemAsync(userId, "Task 3", priority: TaskPriority.Medium, dueDate: new DateOnly(2026, 1, 3));
        await _database.SeedItemAsync(userId, "Task 4", priority: TaskPriority.Low, dueDate: new DateOnly(2026, 1, 4));
        await _database.SeedItemAsync(userId, "Task 5", priority: TaskPriority.High, dueDate: new DateOnly(2026, 1, 5));
        await _database.SeedItemAsync(userId, "Completed task", priority: TaskPriority.Low, dueDate: new DateOnly(2026, 1, 6), isCompleted: true);
        await _database.SeedItemAsync(Guid.NewGuid(), "Other user's task", priority: TaskPriority.Low, dueDate: new DateOnly(2026, 1, 1));
    }
}
