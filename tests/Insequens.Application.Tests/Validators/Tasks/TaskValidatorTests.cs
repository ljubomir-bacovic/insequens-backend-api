using FluentAssertions;
using Insequens.Application.Commands.Tasks;
using Insequens.Application.Queries.Tasks;
using Insequens.Application.Tests.Support;
using Insequens.Application.Validators.Tasks;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using Microsoft.Extensions.Time.Testing;

namespace Insequens.Application.Tests.Validators.Tasks;

public class TaskValidatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly FakeTimeProvider _clock = new(TestDbContextFactory.StartTime);

    [Fact]
    public void CreateTask_WithValidCommand_ReturnsNoErrors()
    {
        var result = new CreateTaskValidator(_clock).Validate(
            new CreateTaskCommand(UserId, "Task", "Details", TaskPriority.High, new DateOnly(2026, 4, 1)));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", null, TaskPriority.None, null, "Name")]
    [InlineData("Task", null, (TaskPriority)9, null, "Priority")]
    [InlineData("Task", null, TaskPriority.None, "2040-01-01", "DueDate")]
    public void CreateTask_WithAnInvalidField_ReturnsAnErrorForIt(
        string name,
        string? description,
        TaskPriority priority,
        string? dueDate,
        string property)
    {
        var result = new CreateTaskValidator(_clock).Validate(
            new CreateTaskCommand(UserId, name, description, priority, dueDate is null ? null : DateOnly.Parse(dueDate)));

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(property);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a101-characters-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void CreateTask_WithAnEmptyOrTooLongIdempotencyKey_ReturnsAnError(string key)
    {
        var result = new CreateTaskValidator(_clock).Validate(
            new CreateTaskCommand(UserId, "Task", null, TaskPriority.None, null, key));

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("IdempotencyKey");
    }

    [Fact]
    public void CreateTask_WithAnIdempotencyKeyOf100Characters_ReturnsNoErrors()
    {
        var result = new CreateTaskValidator(_clock).Validate(
            new CreateTaskCommand(UserId, "Task", null, TaskPriority.None, null, new string('k', 100)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTask_WithADescriptionOver4000Characters_ReturnsAnError()
    {
        var result = new CreateTaskValidator(_clock).Validate(
            new CreateTaskCommand(UserId, "Task", new string('a', 4001), TaskPriority.None, null));

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Description");
    }

    [Fact]
    public void UpdateTask_WithNothingPresentOrExplicitNulls_ReturnsNoErrors()
    {
        var validator = new UpdateTaskValidator(_clock);

        validator.Validate(Update()).IsValid.Should().BeTrue();
        validator.Validate(Update(description: new Optional<string?>(null), dueDate: new Optional<DateOnly?>(null))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTask_WithAnInvalidPresentField_ReturnsAnErrorNamedForTheField()
    {
        var validator = new UpdateTaskValidator(_clock);

        validator.Validate(Update(name: "")).Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Name");
        validator.Validate(Update(name: new string('a', 201))).Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Name");
        validator.Validate(Update(description: new string('a', 4001))).Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Description");
        validator.Validate(Update(priority: (TaskPriority)9)).Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Priority");
        validator.Validate(Update(dueDate: new DateOnly(2040, 1, 1))).Errors.Should().ContainSingle().Which.PropertyName.Should().Be("DueDate");
    }

    [Fact]
    public void ListTasks_WithDefaults_ReturnsNoErrors()
    {
        new ListTasksValidator().Validate(new ListTasksQuery(UserId)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListTasks_WithAnInvalidField_ReturnsAnErrorForIt()
    {
        var validator = new ListTasksValidator();

        PropertyIn(validator, new ListTasksQuery(UserId, Search: new string('a', 101))).Should().Be("Search");
        PropertyIn(validator, new ListTasksQuery(UserId, SortBy: (TaskSortField)9)).Should().Be("SortBy");
        PropertyIn(validator, new ListTasksQuery(UserId, SortDirection: (SortDirection)9)).Should().Be("SortDirection");
        PropertyIn(validator, new ListTasksQuery(UserId, Priority: (TaskPriority)9)).Should().Be("Priority");
        PropertyIn(validator, new ListTasksQuery(UserId, Page: 0)).Should().Be("Page");
        PropertyIn(validator, new ListTasksQuery(UserId, PageSize: 101)).Should().Be("PageSize");
        PropertyIn(validator, new ListTasksQuery(UserId, DueFrom: new DateOnly(2026, 5, 2), DueTo: new DateOnly(2026, 5, 1))).Should().Be("DueTo");
    }

    private static string PropertyIn(ListTasksValidator validator, ListTasksQuery query) =>
        validator.Validate(query).Errors.Should().ContainSingle().Subject.PropertyName;

    private static UpdateTaskCommand Update(
        string? name = null,
        Optional<string?> description = default,
        TaskPriority? priority = null,
        Optional<DateOnly?> dueDate = default) =>
        new(Guid.NewGuid(), UserId, name, description, priority, dueDate);
}
