using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;

namespace Insequens.Application.Tests.Commands;

public sealed class ToggleToDoItemCompleteHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Send_WithOwnedItem_TogglesCompletion(bool initialValue, bool expectedValue)
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, isCompleted: initialValue);
        _database.Clock.Advance(TimeSpan.FromMinutes(5));

        await _database.SendAsync(new ToggleToDoItemCompleteCommand(item.Id, userId));

        var stored = await _database.FindItemAsync(item.Id);
        stored!.IsCompleted.Should().Be(expectedValue);
        stored.UpdatedOn.Should().Be(TestDbContextFactory.StartTime.AddMinutes(5).UtcDateTime);
        stored.CreatedOn.Should().Be(TestDbContextFactory.StartTime.UtcDateTime);
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsToDoItemNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => _database.SendAsync(new ToggleToDoItemCompleteCommand(itemId, Guid.NewGuid()));

        (await action.Should().ThrowAsync<ToDoItemNotFoundException>()).Which.Id.Should().Be(itemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsResourceForbiddenExceptionAndKeepsItem()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => _database.SendAsync(new ToggleToDoItemCompleteCommand(item.Id, Guid.NewGuid()));

        (await action.Should().ThrowAsync<ResourceForbiddenException>()).Which.Id.Should().Be(item.Id);
        (await _database.FindItemAsync(item.Id))!.IsCompleted.Should().BeFalse();
    }
}
