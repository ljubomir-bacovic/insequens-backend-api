using FluentAssertions;
using FluentValidation.Results;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Tests.Support;
using Insequens.Application.Validators.ToDoItem;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Application.Tests.Validators;

public class UpdateToDoItemDueDateValidatorTests
{
    private readonly FakeTimeProvider _clock = new(TestDbContextFactory.StartTime);

    [Theory]
    [InlineData(null)]
    [InlineData("2016-03-01")]
    [InlineData("2026-03-01")]
    [InlineData("2036-03-01")]
    public void Validate_WithNoDueDateOrOneWithinTenYears_ReturnsNoErrors(string? dueDate)
    {
        var result = Validate(dueDate);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2016-02-29")]
    [InlineData("2036-03-02")]
    public void Validate_WithDueDateMoreThanTenYearsFromToday_ReturnsError(string dueDate)
    {
        var result = Validate(dueDate);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "DueDate" && error.ErrorMessage == "Due date must be within 10 years of today.");
    }

    [Fact]
    public void Validate_AfterTheClockMoves_UsesTheCurrentDate()
    {
        _clock.Advance(TimeSpan.FromDays(1));

        var result = Validate("2036-03-02");

        result.IsValid.Should().BeTrue();
    }

    private ValidationResult Validate(string? dueDate) =>
        new UpdateToDoItemDueDateValidator(_clock).Validate(
            new UpdateToDoItemDueDateCommand(Guid.NewGuid(), Guid.NewGuid(), dueDate is null ? null : DateOnly.Parse(dueDate)));
}
