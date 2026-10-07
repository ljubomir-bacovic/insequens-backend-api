using FluentAssertions;
using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V1.Tasks;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Tests.Commands;

public sealed class UpdateToDoItemPriorityHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData(TaskPriority.High, DomainPriority.High)]
    [InlineData(TaskPriority.Medium, DomainPriority.Medium)]
    [InlineData(TaskPriority.Low, DomainPriority.Low)]
    public async Task Send_WithOwnedItem_UpdatesPriority(TaskPriority priority, DomainPriority expected)
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId);

        await _database.SendAsync(new UpdateToDoItemPriorityCommand(item.Id, userId, priority));

        (await _database.FindItemAsync(item.Id))!.Priority.Should().Be(expected);
    }

    [Fact]
    public async Task Send_WithInvalidPriority_ThrowsValidationExceptionAndKeepsPriority()
    {
        var userId = Guid.NewGuid();
        var item = await _database.SeedItemAsync(userId, priority: DomainPriority.Low);

        var action = () => _database.SendAsync(new UpdateToDoItemPriorityCommand(item.Id, userId, (TaskPriority)99));

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "Priority" &&
            error.ErrorMessage == "Priority must be one of: 1 (high), 2 (medium), or 3 (low).");
        (await _database.FindItemAsync(item.Id))!.Priority.Should().Be(DomainPriority.Low);
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsNotFoundException()
    {
        var itemId = Guid.NewGuid();

        var action = () => _database.SendAsync(new UpdateToDoItemPriorityCommand(itemId, Guid.NewGuid(), TaskPriority.High));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(itemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsNotFoundExceptionAndKeepsPriority()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid(), priority: DomainPriority.Low);

        var action = () => _database.SendAsync(new UpdateToDoItemPriorityCommand(item.Id, Guid.NewGuid(), TaskPriority.High));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(item.Id);
        (await _database.FindItemAsync(item.Id))!.Priority.Should().Be(DomainPriority.Low);
    }
}
