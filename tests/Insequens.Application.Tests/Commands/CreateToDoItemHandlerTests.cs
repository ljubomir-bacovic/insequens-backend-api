using FluentAssertions;
using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V1.Tasks;
using Microsoft.EntityFrameworkCore;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Tests.Commands;

public sealed class CreateToDoItemHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Send_WithValidCommand_PersistsItemAndReturnsDetails()
    {
        var userId = Guid.NewGuid();
        var request = new CreateToDoItemCommand("Task", "Description", 2, new DateOnly(2026, 1, 1), userId);

        var result = await _database.SendAsync(request);

        var item = await _database.FindItemAsync(result.Id);
        item.Should().NotBeNull();
        item!.UserId.Should().Be(userId);
        item.Name.Should().Be(request.Name);
        item.Description.Should().Be(request.Description);
        item.DueDate.Should().Be(request.DueDate);
        item.Priority.Should().Be(DomainPriority.Medium);
        item.IsCompleted.Should().BeFalse();
        item.CreatedOn.Should().Be(TestDbContextFactory.StartTime.UtcDateTime);
        result.Should().Be(new ToDoItemGetDetailsModel(
            item.Id,
            request.Name,
            request.Description,
            TaskPriority.Medium,
            request.DueDate,
            false));
    }

    [Fact]
    public async Task Send_WithInvalidCommand_ThrowsValidationExceptionAndPersistsNothing()
    {
        var action = () => _database.SendAsync(new CreateToDoItemCommand(string.Empty, null, 0, null, Guid.NewGuid()));

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "Name" &&
            error.ErrorMessage == "Task name is required.");
        await using var context = _database.CreateContext();
        (await context.ToDoItems.CountAsync()).Should().Be(0);
    }
}
