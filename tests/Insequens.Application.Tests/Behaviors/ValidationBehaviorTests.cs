using FluentAssertions;
using FluentValidation;
using Insequens.Application.Behaviors;
using Insequens.Application.Models;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Application.Validators.ToDoItem;
using Insequens.Domain.Model.ToDoItem;
using MediatR;

namespace Insequens.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithValidRequest_CallsNext()
    {
        var request = new TestRequest("Valid name", 1);
        var validators = new IValidator<TestRequest>[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, Unit>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<Unit> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(Unit.Value);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationExceptionWithFailuresFromAllValidators()
    {
        var request = new TestRequest(string.Empty, 0);
        var validators = new IValidator<TestRequest>[]
        {
            new TestRequestValidator(),
            new AdditionalTestRequestValidator()
        };
        var behavior = new ValidationBehavior<TestRequest, Unit>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<Unit> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        };

        var action = () => behavior.Handle(request, next, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        var errorsByProperty = exception.Which.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        errorsByProperty.Should().ContainKey(nameof(TestRequest.Name));
        errorsByProperty[nameof(TestRequest.Name)].Should().BeEquivalentTo(
            "Name is required.",
            "Name must be at least 3 characters.");
        errorsByProperty.Should().ContainKey(nameof(TestRequest.Quantity));
        errorsByProperty[nameof(TestRequest.Quantity)].Should().BeEquivalentTo("Quantity must be greater than 0.");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithoutValidators_CallsNext()
    {
        var request = new TestRequest("Valid name", 1);
        var behavior = new ValidationBehavior<TestRequest, Unit>([]);
        var nextCalled = false;
        RequestHandlerDelegate<Unit> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(Unit.Value);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidGetUserToDoItemsQuery_ThrowsValidationExceptionAndDoesNotCallNext()
    {
        var request = new GetUserToDoItemsQuery(Guid.NewGuid(), false, 0, 101);
        var behavior = new ValidationBehavior<GetUserToDoItemsQuery, PaginatedResult<ToDoItemGetListModel>>([new GetUserToDoItemsValidator()]);
        var nextCalled = false;
        RequestHandlerDelegate<PaginatedResult<ToDoItemGetListModel>> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult(new PaginatedResult<ToDoItemGetListModel>([], 0, 1, 20));
        };

        var action = () => behavior.Handle(request, next, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().Contain(error => error.PropertyName == nameof(GetUserToDoItemsQuery.Page) && error.ErrorMessage == "Page must be greater than 0.");
        exception.Which.Errors.Should().Contain(error => error.PropertyName == nameof(GetUserToDoItemsQuery.PageSize) && error.ErrorMessage == "PageSize must be between 1 and 100.");
        nextCalled.Should().BeFalse();
    }

    private sealed record TestRequest(string Name, int Quantity) : IRequest<Unit>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty()
                .WithMessage("Name is required.");

            RuleFor(request => request.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than 0.");
        }
    }

    private sealed class AdditionalTestRequestValidator : AbstractValidator<TestRequest>
    {
        public AdditionalTestRequestValidator()
        {
            RuleFor(request => request.Name)
                .MinimumLength(3)
                .WithMessage("Name must be at least 3 characters.");
        }
    }
}
