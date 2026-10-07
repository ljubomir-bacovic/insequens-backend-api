using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;

namespace Insequens.Application.Tests.Commands;

public sealed class UpdateToDoItemDueDateHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public static TheoryData<DateOnly> DueDates =>
    [
        new DateOnly(2016, 3, 1),
        new DateOnly(2026, 1, 1),
        new DateOnly(2036, 3, 1),
    ];

    public void Dispose() => _database.Dispose();

    [Theory]
    [MemberData(nameof(DueDates))]
    public async Task Send_WithOwnedItem_UpdatesDueDate(DateOnly dueDate)
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId);

        await _database.SendAsync(new UpdateToDoItemDueDateCommand(item.Id, userId, dueDate));

        (await _database.FindItemAsync(item.Id))!.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public async Task Send_WithNullDueDate_ClearsDueDate()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, dueDate: new DateOnly(2026, 1, 1));

        await _database.SendAsync(new UpdateToDoItemDueDateCommand(item.Id, userId, null));

        (await _database.FindItemAsync(item.Id))!.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Send_WithDueDateOutOfRange_ThrowsValidationExceptionAndKeepsDueDate()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, dueDate: new DateOnly(2026, 1, 1));

        var action = () => _database.SendAsync(new UpdateToDoItemDueDateCommand(item.Id, userId, DateOnly.MaxValue));

        await action.Should().ThrowAsync<FluentValidation.ValidationException>();
        (await _database.FindItemAsync(item.Id))!.DueDate.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => _database.SendAsync(new UpdateToDoItemDueDateCommand(itemId, Guid.NewGuid(), new DateOnly(2026, 1, 1)));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(itemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsNotFoundExceptionAndKeepsDueDate()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => _database.SendAsync(new UpdateToDoItemDueDateCommand(item.Id, Guid.NewGuid(), new DateOnly(2026, 1, 1)));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(item.Id);
        (await _database.FindItemAsync(item.Id))!.DueDate.Should().BeNull();
    }
}
