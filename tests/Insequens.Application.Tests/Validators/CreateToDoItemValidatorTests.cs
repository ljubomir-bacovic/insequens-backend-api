using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Validators.ToDoItem;

namespace Insequens.Application.Tests.Validators;

public class CreateToDoItemValidatorTests
{
    private readonly CreateToDoItemValidator _validator = new();

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
}
