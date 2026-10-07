using FluentAssertions;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Exceptions;
using Insequens.Application.Queries.Tasks;
using Insequens.Application.Tests.Support;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Tests.Commands.Tasks;

/// <summary>The v2 task requests: create, get, partial update and completion.</summary>
public sealed class TaskCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _database = new();
    private readonly Guid _userId = Guid.NewGuid();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Create_WithAPriority_StoresTheDomainValueAndReturnsIt()
    {
        await _database.SeedUserAsync(_userId);

        var created = await _database.SendAsync(new CreateTaskCommand(_userId, "Task", "Details", TaskPriority.High, new DateOnly(2026, 4, 1)));

        created.Should().BeEquivalentTo(new { Name = "Task", Description = "Details", Priority = TaskPriority.High, DueDate = new DateOnly(2026, 4, 1), IsCompleted = false });
        (await _database.FindItemAsync(created.Id))!.Priority.Should().Be(DomainPriority.High);
    }

    [Fact]
    public async Task Create_WithAnUnknownPriorityOrEmptyName_ThrowsValidationException()
    {
        var unknownPriority = () => _database.SendAsync(new CreateTaskCommand(_userId, "Task", null, (TaskPriority)9, null));
        var emptyName = () => _database.SendAsync(new CreateTaskCommand(_userId, "", null, TaskPriority.None, null));

        await unknownPriority.Should().ThrowAsync<FluentValidation.ValidationException>();
        await emptyName.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Get_ReturnsTheTaskWithItsVersion()
    {
        var item = await _database.SeedItemAsync(_userId, "Task", priority: DomainPriority.Low);

        var result = await _database.SendAsync(new GetTaskQuery(item.Id, _userId));

        result.Value.Priority.Should().Be(TaskPriority.Low);
        result.Version.Should().Equal(item.RowVersion);
    }

    [Fact]
    public async Task Get_OtherUsersTask_ThrowsNotFound()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => _database.SendAsync(new GetTaskQuery(item.Id, _userId));

        (await action.Should().ThrowAsync<NotFoundException>()).Which.Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task Update_WithEveryField_ChangesEveryField()
    {
        var item = await SeedFullTaskAsync();

        await _database.SendAsync(new UpdateTaskCommand(
            item.Id, _userId, "Renamed", "New details", TaskPriority.Low, new DateOnly(2026, 6, 1)));

        var updated = (await _database.FindItemAsync(item.Id))!;
        updated.Name.Should().Be("Renamed");
        updated.Description.Should().Be("New details");
        updated.Priority.Should().Be(DomainPriority.Low);
        updated.DueDate.Should().Be(new DateOnly(2026, 6, 1));
    }

    [Fact]
    public async Task Update_WithNothingPresent_ChangesNothing()
    {
        var item = await SeedFullTaskAsync();

        await _database.SendAsync(new UpdateTaskCommand(item.Id, _userId, null, default, null, default));

        (await _database.FindItemAsync(item.Id)).Should().BeEquivalentTo(item, options => options.Excluding(task => task.UpdatedOn));
    }

    [Fact]
    public async Task Update_WithExplicitNulls_ClearsTheDescriptionAndDueDateOnly()
    {
        var item = await SeedFullTaskAsync();

        await _database.SendAsync(new UpdateTaskCommand(
            item.Id, _userId, null, new Optional<string?>(null), null, new Optional<DateOnly?>(null)));

        var updated = (await _database.FindItemAsync(item.Id))!;
        updated.Description.Should().BeNull();
        updated.DueDate.Should().BeNull();
        updated.Name.Should().Be("Task");
        updated.Priority.Should().Be(DomainPriority.High);
    }

    [Fact]
    public async Task Update_WithAStaleVersion_ThrowsPreconditionFailed()
    {
        var item = await SeedFullTaskAsync();

        var action = () => _database.SendAsync(new UpdateTaskCommand(item.Id, _userId, "Renamed", default, null, default, [1, 2, 3]));

        await action.Should().ThrowAsync<PreconditionFailedException>();
        (await _database.FindItemAsync(item.Id))!.Name.Should().Be("Task");
    }

    [Fact]
    public async Task Update_OtherUsersTask_ThrowsNotFound()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => _database.SendAsync(new UpdateTaskCommand(item.Id, _userId, "Renamed", default, null, default));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_WithTheCurrentVersion_RemovesTheTask()
    {
        var item = await SeedFullTaskAsync();

        await _database.SendAsync(new DeleteTaskCommand(item.Id, _userId, item.RowVersion));

        (await _database.FindItemAsync(item.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_WithAStaleVersionOrAnotherUsersTask_KeepsTheTask()
    {
        var item = await SeedFullTaskAsync();

        var stale = () => _database.SendAsync(new DeleteTaskCommand(item.Id, _userId, [1, 2, 3]));
        var foreign = () => _database.SendAsync(new DeleteTaskCommand(item.Id, Guid.NewGuid()));

        await stale.Should().ThrowAsync<PreconditionFailedException>();
        await foreign.Should().ThrowAsync<NotFoundException>();
        (await _database.FindItemAsync(item.Id)).Should().NotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetCompletion_Twice_LeavesTheSameState(bool completed)
    {
        var item = await _database.SeedItemAsync(_userId, isCompleted: !completed);

        await _database.SendAsync(new SetTaskCompletionCommand(item.Id, _userId, completed));
        await _database.SendAsync(new SetTaskCompletionCommand(item.Id, _userId, completed));

        (await _database.FindItemAsync(item.Id))!.IsCompleted.Should().Be(completed);
    }

    [Fact]
    public async Task SetCompletion_OtherUsersTask_ThrowsNotFound()
    {
        var item = await _database.SeedItemAsync(Guid.NewGuid());

        var action = () => _database.SendAsync(new SetTaskCompletionCommand(item.Id, _userId, true));

        await action.Should().ThrowAsync<NotFoundException>();
        (await _database.FindItemAsync(item.Id))!.IsCompleted.Should().BeFalse();
    }

    private Task<Domain.Entities.ToDoItem> SeedFullTaskAsync() =>
        _database.SeedItemAsync(_userId, "Task", "Details", DomainPriority.High, new DateOnly(2026, 4, 1));
}
