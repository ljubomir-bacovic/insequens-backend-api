using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Validators.ToDoItem;

namespace Insequens.Application.Tests.Validators;

public class UpdateToDoItemNameValidatorTests
{
    private readonly UpdateToDoItemNameValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new UpdateToDoItemNameCommand(Guid.NewGuid(), Guid.NewGuid(), "Task"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var result = _validator.Validate(new UpdateToDoItemNameCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Name" && error.ErrorMessage == "Task name is required.");
    }

    [Fact]
    public void Validate_WithNameLongerThan200Characters_ReturnsError()
    {
        var result = _validator.Validate(new UpdateToDoItemNameCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 201)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Name" && error.ErrorMessage == "Task name must not exceed 200 characters.");
    }
}
