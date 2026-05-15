using FluentAssertions;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Application.Validators.ToDoItem;

namespace Insequens.Application.Tests.Validators;

public class GetUserToDoItemsValidatorTests
{
    private readonly GetUserToDoItemsValidator _validator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 20)]
    [InlineData(3, 100)]
    public void Validate_WithValidPagination_ReturnsNoErrors(int page, int pageSize)
    {
        var result = _validator.Validate(new GetUserToDoItemsQuery(Guid.NewGuid(), false, page, pageSize));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPage_ReturnsError(int page)
    {
        var result = _validator.Validate(new GetUserToDoItemsQuery(Guid.NewGuid(), false, page, 20));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Page" && error.ErrorMessage == "Page must be greater than 0.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithInvalidPageSize_ReturnsError(int pageSize)
    {
        var result = _validator.Validate(new GetUserToDoItemsQuery(Guid.NewGuid(), false, 1, pageSize));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "PageSize" && error.ErrorMessage == "PageSize must be between 1 and 100.");
    }
}
