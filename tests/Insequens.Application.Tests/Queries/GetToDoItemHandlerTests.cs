using FluentAssertions;
using Insequens.Application.Exceptions;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V1.Tasks;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Tests.Queries;

public sealed class GetToDoItemHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithOwnedItem_ReturnsDetails()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(
            userId,
            name: "Task",
            description: "Description",
            priority: DomainPriority.High,
            dueDate: new DateOnly(2026, 2, 1));

        var result = await _database.SendAsync(new GetToDoItemQuery(item.Id, userId));

        result.Should().Be(new ToDoItemGetDetailsModel(
            item.Id,
            "Task",
            "Description",
            TaskPriority.High,
            new DateOnly(2026, 2, 1),
            false));
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsToDoItemNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => _database.SendAsync(new GetToDoItemQuery(itemId, Guid.NewGuid()));

        (await action.Should().ThrowAsync<ToDoItemNotFoundException>()).Which.Id.Should().Be(itemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsResourceForbiddenException()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => _database.SendAsync(new GetToDoItemQuery(item.Id, Guid.NewGuid()));

        (await action.Should().ThrowAsync<ResourceForbiddenException>()).Which.Id.Should().Be(item.Id);
    }
}
