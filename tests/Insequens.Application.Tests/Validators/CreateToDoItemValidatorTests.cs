using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Tests.Support;
using Insequens.Application.Validators.ToDoItem;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Application.Tests.Validators;

public class CreateToDoItemValidatorTests
{
    private readonly CreateToDoItemValidator _validator = new(new FakeTimeProvider(TestDbContextFactory.StartTime));

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new CreateToDoItemCommand("Task", "Description", 2, new DateOnly(2026, 1, 1), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var result = _validator.Validate(new CreateToDoItemCommand(string.Empty, "Description", 2, new DateOnly(2026, 1, 1), Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Name" && error.ErrorMessage == "Task name is required.");
    }

    [Fact]
    public void Validate_WithNameLongerThan200Characters_ReturnsError()
    {
        var result = _validator.Validate(new CreateToDoItemCommand(new string('a', 201), "Description", 2, new DateOnly(2026, 1, 1), Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Name" && error.ErrorMessage == "Task name must not exceed 200 characters.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Validate_WithPriorityOutOfRange_ReturnsError(int priority)
    {
        var result = _validator.Validate(new CreateToDoItemCommand("Task", "Description", priority, new DateOnly(2026, 1, 1), Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Priority" && error.ErrorMessage == "Priority must be one of: 0 (none), 1 (high), 2 (medium), or 3 (low).");
    }

    [Fact]
    public void Validate_WithDescriptionLongerThan4000Characters_ReturnsError()
    {
        var result = _validator.Validate(new CreateToDoItemCommand("Task", new string('a', 4001), 2, null, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Description" && error.ErrorMessage == "Task description must not exceed 4000 characters.");
    }

    [Theory]
    [InlineData("2016-03-01")]
    [InlineData("2036-03-01")]
    public void Validate_WithDueDateTenYearsFromToday_ReturnsNoErrors(string dueDate)
    {
        var result = _validator.Validate(new CreateToDoItemCommand("Task", null, 2, DateOnly.Parse(dueDate), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2016-02-29")]
    [InlineData("2036-03-02")]
    public void Validate_WithDueDateMoreThanTenYearsFromToday_ReturnsError(string dueDate)
    {
        var result = _validator.Validate(new CreateToDoItemCommand("Task", null, 2, DateOnly.Parse(dueDate), Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "DueDate" && error.ErrorMessage == "Due date must be within 10 years of today.");
    }
}
