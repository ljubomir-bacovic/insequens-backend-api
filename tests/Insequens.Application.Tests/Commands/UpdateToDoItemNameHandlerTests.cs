using FluentAssertions;
using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;

namespace Insequens.Application.Tests.Commands;

public sealed class UpdateToDoItemNameHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithOwnedItem_UpdatesName()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, name: "Original task name");

        await _database.SendAsync(new UpdateToDoItemNameCommand(item.Id, userId, "Updated task name"));

        (await _database.FindItemAsync(item.Id))!.Name.Should().Be("Updated task name");
    }

    public static TheoryData<string?, string> InvalidNames => new()
    {
        { string.Empty, "Task name is required." },
        { null, "Task name is required." },
        { new string('a', 201), "Task name must not exceed 200 characters." },
    };

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public async Task Send_WithInvalidName_ThrowsValidationExceptionAndKeepsName(string? name, string expectedMessage)
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, name: "Original task name");

        var action = () => _database.SendAsync(new UpdateToDoItemNameCommand(item.Id, userId, name!));

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "Name" &&
            error.ErrorMessage == expectedMessage);
        (await _database.FindItemAsync(item.Id))!.Name.Should().Be("Original task name");
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => _database.SendAsync(new UpdateToDoItemNameCommand(itemId, Guid.NewGuid(), "Updated task name"));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(itemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsNotFoundExceptionAndKeepsName()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid(), name: "Original task name");

        var action = () => _database.SendAsync(new UpdateToDoItemNameCommand(item.Id, Guid.NewGuid(), "Updated task name"));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(item.Id);
        (await _database.FindItemAsync(item.Id))!.Name.Should().Be("Original task name");
    }
}
