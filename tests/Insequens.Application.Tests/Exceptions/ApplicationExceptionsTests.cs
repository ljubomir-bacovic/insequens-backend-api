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
    public void ForbiddenException_WithRole_NamesTheRole()
    {
        var exception = new ForbiddenException("Admin");

        exception.RequiredRole.Should().Be("Admin");
        exception.Message.Should().Be("The Admin role is required.");
        exception.Should().NotBeAssignableTo<ResourceException>();
    }
}
