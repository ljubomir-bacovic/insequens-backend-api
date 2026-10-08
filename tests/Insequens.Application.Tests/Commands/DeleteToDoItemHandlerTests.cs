using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Application.Tests.Support;

namespace Insequens.Application.Tests.Commands;

public sealed class DeleteToDoItemHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithOwnedItem_MovesItToTheTrash()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId);

        await _database.SendAsync(new DeleteToDoItemCommand(item.Id, userId));

        (await _database.FindItemAsync(item.Id))!.IsDeleted.Should().BeTrue();
        var get = () => _database.SendAsync(new GetToDoItemQuery(item.Id, userId));
        await get.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => _database.SendAsync(new DeleteToDoItemCommand(itemId, Guid.NewGuid()));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(itemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsNotFoundExceptionAndKeepsItem()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => _database.SendAsync(new DeleteToDoItemCommand(item.Id, Guid.NewGuid()));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(item.Id);
        (await _database.FindItemAsync(item.Id))!.IsDeleted.Should().BeFalse();
    }
}
