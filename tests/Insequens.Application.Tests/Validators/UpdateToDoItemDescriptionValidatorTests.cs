using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Validators.ToDoItem;

namespace Insequens.Application.Tests.Validators;

public class UpdateToDoItemDescriptionValidatorTests
{
    private readonly UpdateToDoItemDescriptionValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Description")]
    public void Validate_WithDescriptionWithinLimit_ReturnsNoErrors(string? description)
    {
        var result = _validator.Validate(new UpdateToDoItemDescriptionCommand(Guid.NewGuid(), Guid.NewGuid(), description));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDescriptionOf4000Characters_ReturnsNoErrors()
    {
        var result = _validator.Validate(new UpdateToDoItemDescriptionCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 4000)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDescriptionLongerThan4000Characters_ReturnsError()
    {
        var result = _validator.Validate(new UpdateToDoItemDescriptionCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 4001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Description" && error.ErrorMessage == "Task description must not exceed 4000 characters.");
    }
}
