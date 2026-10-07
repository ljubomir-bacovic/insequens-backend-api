using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Insequens.Domain.Exceptions;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Commands;

public sealed class UpdateToDoItemDescriptionHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData("Updated task description")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Send_WithOwnedItem_UpdatesDescription(string? description)
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, description: "Original task description");

        await _database.SendAsync(new UpdateToDoItemDescriptionCommand(item.Id, userId, description));

        (await _database.FindItemAsync(item.Id))!.Description.Should().Be(description);
    }

    [Fact]
    public async Task Send_WithDescriptionOverMaximumLength_ThrowsDomainExceptionAndKeepsDescription()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, description: "Original task description");
        var description = new string('a', ToDoItemEntity.DescriptionMaxLength + 1);

        var action = () => _database.SendAsync(new UpdateToDoItemDescriptionCommand(item.Id, userId, description));

        await action.Should().ThrowAsync<ToDoItemDescriptionTooLongException>();
        (await _database.FindItemAsync(item.Id))!.Description.Should().Be("Original task description");
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => _database.SendAsync(new UpdateToDoItemDescriptionCommand(itemId, Guid.NewGuid(), "Description"));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(itemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsNotFoundExceptionAndKeepsDescription()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid(), description: "Original task description");

        var action = () => _database.SendAsync(new UpdateToDoItemDescriptionCommand(item.Id, Guid.NewGuid(), "Changed"));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(item.Id);
        (await _database.FindItemAsync(item.Id))!.Description.Should().Be("Original task description");
    }
}
