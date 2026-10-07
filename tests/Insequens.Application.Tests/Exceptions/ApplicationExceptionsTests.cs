using FluentAssertions;
using Insequens.Application.Exceptions;

namespace Insequens.Application.Tests.Exceptions;

public class ApplicationExceptionsTests
{
    [Fact]
    public void NotFoundException_WithResourceAndId_SetsBothAndMessage()
    {
        var itemId = Guid.NewGuid();

        var exception = new NotFoundException("ToDoItem", itemId);

        exception.ResourceName.Should().Be("ToDoItem");
        exception.Id.Should().Be(itemId);
        exception.Message.Should().Be($"ToDoItem {itemId} was not found.");
        exception.Should().BeAssignableTo<ResourceException>();
    }

    [Fact]
    public void ResourceForbiddenException_WithId_SetsIdAndMessage()
    {
        var itemId = Guid.NewGuid();

        var exception = new ResourceForbiddenException(itemId);

        exception.Id.Should().Be(itemId);
        exception.Message.Should().Be($"Access denied for resource {itemId}.");
        exception.Should().BeAssignableTo<ResourceException>();
    }
}
