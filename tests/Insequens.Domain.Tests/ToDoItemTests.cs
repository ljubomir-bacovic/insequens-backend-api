using FluentAssertions;
using Insequens.Domain.Entities;
using Insequens.Domain.Exceptions;
using Insequens.Domain.Types;

namespace Insequens.Domain.Tests;

public class ToDoItemTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidValues_SetsEveryFieldAndANewId()
    {
        var item = ToDoItem.Create(UserId, "Task", "Description", TaskPriority.High, new DateOnly(2026, 5, 1));

        item.Id.Should().NotBeEmpty();
        item.UserId.Should().Be(UserId);
        item.Name.Should().Be("Task");
        item.Description.Should().Be("Description");
        item.Priority.Should().Be(TaskPriority.High);
        item.DueDate.Should().Be(new DateOnly(2026, 5, 1));
        item.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOptionalValuesOmitted_LeavesThemNull()
    {
        var item = ToDoItem.Create(UserId, "Task", null, null, null);

        item.Description.Should().BeNull();
        item.Priority.Should().BeNull();
        item.DueDate.Should().BeNull();
    }

    [Fact]
    public void Create_CalledTwice_GivesDistinctIds()
    {
        var first = ToDoItem.Create(UserId, "Task", null, null, null);
        var second = ToDoItem.Create(UserId, "Task", null, null, null);

        first.Id.Should().NotBe(second.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutName_ThrowsInvalidToDoItemNameException(string? name)
    {
        var action = () => ToDoItem.Create(UserId, name!, null, null, null);

        action.Should().Throw<InvalidToDoItemNameException>().WithMessage("Task name is required.");
    }

    [Fact]
    public void Create_WithTooLongDescription_ThrowsToDoItemDescriptionTooLongException()
    {
        var action = () => ToDoItem.Create(UserId, "Task", new string('a', ToDoItem.DescriptionMaxLength + 1), null, null);

        action.Should().Throw<ToDoItemDescriptionTooLongException>();
    }

    [Fact]
    public void Rename_WithValidName_ChangesName()
    {
        var item = NewItem();

        item.Rename("Renamed");

        item.Name.Should().Be("Renamed");
    }

    [Fact]
    public void Rename_WithNameAtMaximumLength_ChangesName()
    {
        var item = NewItem();
        var name = new string('a', ToDoItem.NameMaxLength);

        item.Rename(name);

        item.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Rename_WithoutName_ThrowsAndKeepsName(string? name)
    {
        var item = NewItem();

        var action = () => item.Rename(name!);

        action.Should().Throw<InvalidToDoItemNameException>().WithMessage("Task name is required.");
        item.Name.Should().Be("Task");
    }

    [Fact]
    public void Rename_WithNameOverMaximumLength_ThrowsAndKeepsName()
    {
        var item = NewItem();

        var action = () => item.Rename(new string('a', ToDoItem.NameMaxLength + 1));

        action.Should().Throw<InvalidToDoItemNameException>()
            .WithMessage($"Task name must not exceed {ToDoItem.NameMaxLength} characters.");
        item.Name.Should().Be("Task");
    }

    [Theory]
    [InlineData("New description")]
    [InlineData("")]
    [InlineData(null)]
    public void UpdateDescription_WithinLimit_ChangesDescription(string? description)
    {
        var item = NewItem();

        item.UpdateDescription(description);

        item.Description.Should().Be(description);
    }

    [Fact]
    public void UpdateDescription_AtMaximumLength_ChangesDescription()
    {
        var item = NewItem();
        var description = new string('a', ToDoItem.DescriptionMaxLength);

        item.UpdateDescription(description);

        item.Description.Should().Be(description);
    }

    [Fact]
    public void UpdateDescription_OverMaximumLength_ThrowsAndKeepsDescription()
    {
        var item = NewItem();

        var action = () => item.UpdateDescription(new string('a', ToDoItem.DescriptionMaxLength + 1));

        action.Should().Throw<ToDoItemDescriptionTooLongException>()
            .WithMessage($"Task description must not exceed {ToDoItem.DescriptionMaxLength} characters.")
            .Which.Should().BeAssignableTo<DomainException>();
        item.Description.Should().Be("Description");
    }

    [Theory]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Low)]
    [InlineData(null)]
    public void ChangePriority_SetsPriority(TaskPriority? priority)
    {
        var item = NewItem();

        item.ChangePriority(priority);

        item.Priority.Should().Be(priority);
    }

    [Fact]
    public void Reschedule_WithDate_SetsDueDate()
    {
        var item = NewItem();

        item.Reschedule(new DateOnly(2027, 1, 1));

        item.DueDate.Should().Be(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public void Reschedule_WithNull_ClearsDueDate()
    {
        var item = NewItem();

        item.Reschedule(null);

        item.DueDate.Should().BeNull();
    }

    [Fact]
    public void MarkCompleted_OnOpenItem_CompletesIt()
    {
        var item = NewItem();

        item.MarkCompleted();

        item.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void MarkCompleted_OnCompletedItem_KeepsItCompleted()
    {
        var item = NewItem();
        item.MarkCompleted();

        item.MarkCompleted();

        item.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Reopen_OnCompletedItem_ReopensIt()
    {
        var item = NewItem();
        item.MarkCompleted();

        item.Reopen();

        item.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Reopen_OnOpenItem_KeepsItOpen()
    {
        var item = NewItem();

        item.Reopen();

        item.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void NewItem_HasNoAuditValuesUntilSaved()
    {
        var item = NewItem();

        item.CreatedOn.Should().Be(default);
        item.CreatedBy.Should().BeNull();
        item.UpdatedOn.Should().Be(default);
        item.UpdatedBy.Should().BeNull();
    }

    private static ToDoItem NewItem() =>
        ToDoItem.Create(UserId, "Task", "Description", TaskPriority.Medium, new DateOnly(2026, 5, 1));
}
