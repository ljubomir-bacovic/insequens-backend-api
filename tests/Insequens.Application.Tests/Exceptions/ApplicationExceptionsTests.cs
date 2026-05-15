using FluentAssertions;
using Insequens.Application.Exceptions;

namespace Insequens.Application.Tests.Exceptions;

public class ApplicationExceptionsTests
{
    [Fact]
    public void ToDoItemNotFoundException_WithId_SetsId()
    {
        var itemId = Guid.NewGuid();

        var exception = new ToDoItemNotFoundException(itemId);

        exception.Id.Should().Be(itemId);
    }

    [Fact]
    public void ToDoItemNotFoundException_WithMessageAndInnerException_PreservesBaseExceptionData()
    {
        var innerException = new InvalidOperationException("inner");

        var exception = new ToDoItemNotFoundException("message", innerException);

        exception.Message.Should().Be("message");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void ResourceForbiddenException_WithId_SetsIdAndMessage()
    {
        var itemId = Guid.NewGuid();

        var exception = new ResourceForbiddenException(itemId);

        exception.Id.Should().Be(itemId);
        exception.Message.Should().Be($"Access denied for resource {itemId}.");
    }
}
