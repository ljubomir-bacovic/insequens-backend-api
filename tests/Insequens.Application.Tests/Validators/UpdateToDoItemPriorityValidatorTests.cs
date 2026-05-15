using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Validators.ToDoItem;
using Insequens.Domain.Types;

namespace Insequens.Application.Tests.Validators;

public class UpdateToDoItemPriorityValidatorTests
{
    private readonly UpdateToDoItemPriorityValidator _validator = new();

    [Theory]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.Low)]
    public void Validate_WithValidPriority_ReturnsNoErrors(TaskPriority priority)
    {
        var result = _validator.Validate(new UpdateToDoItemPriorityCommand(Guid.NewGuid(), Guid.NewGuid(), priority));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(99)]
    public void Validate_WithInvalidPriority_ReturnsError(int priority)
    {
        var result = _validator.Validate(new UpdateToDoItemPriorityCommand(Guid.NewGuid(), Guid.NewGuid(), (TaskPriority)priority));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Priority" && error.ErrorMessage == "Priority must be one of: 1 (high), 2 (medium), or 3 (low).");
    }
}
