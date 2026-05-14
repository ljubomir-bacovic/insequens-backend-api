using FluentAssertions;
using Insequens.Application.Behaviors;
using Insequens.Application.Commands;
using Insequens.Core.Exceptions;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Entities;
using MediatR;
using NSubstitute;

namespace Insequens.Application.Tests.Behaviors;

public class OwnershipBehaviorTests
{
    [Fact]
    public async Task Handle_WithOwnedItem_CallsNext()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new TestOwnedRequest(userId, itemId);
        var repository = Substitute.For<IRepository<ToDoItem>>();
        var dataContext = Substitute.For<IDataContext>();
        var behavior = new OwnershipBehavior<TestOwnedRequest, Unit>(dataContext);
        var nextCalled = false;

        repository.FindAsync(request.ItemId).Returns(new ToDoItem { Id = itemId, UserId = userId });
        dataContext.GetRepository<ToDoItem>().Returns(repository);
        RequestHandlerDelegate<Unit> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(Unit.Value);
        nextCalled.Should().BeTrue();
        await repository.Received(1).FindAsync(request.ItemId);
    }

    [Fact]
    public async Task Handle_WithNonexistentItem_ThrowsToDoItemNotFoundException()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new TestOwnedRequest(userId, itemId);
        var repository = Substitute.For<IRepository<ToDoItem>>();
        var dataContext = Substitute.For<IDataContext>();
        var behavior = new OwnershipBehavior<TestOwnedRequest, Unit>(dataContext);
        var nextCalled = false;

        repository.FindAsync(request.ItemId).Returns((ToDoItem?)null);
        dataContext.GetRepository<ToDoItem>().Returns(repository);
        RequestHandlerDelegate<Unit> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        };

        var action = () => behavior.Handle(request, next, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ToDoItemNotFoundException>();
        exception.Which.Id.Should().Be(itemId);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithOtherUsersItem_ThrowsResourceForbiddenException()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new TestOwnedRequest(userId, itemId);
        var repository = Substitute.For<IRepository<ToDoItem>>();
        var dataContext = Substitute.For<IDataContext>();
        var behavior = new OwnershipBehavior<TestOwnedRequest, Unit>(dataContext);
        var nextCalled = false;

        repository.FindAsync(request.ItemId).Returns(new ToDoItem { Id = itemId, UserId = Guid.NewGuid() });
        dataContext.GetRepository<ToDoItem>().Returns(repository);
        RequestHandlerDelegate<Unit> next = cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        };

        var action = () => behavior.Handle(request, next, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ResourceForbiddenException>();
        exception.Which.Id.Should().Be(itemId);
        nextCalled.Should().BeFalse();
    }

    private sealed record TestOwnedRequest(Guid UserId, Guid ItemId) : IRequest<Unit>, IOwned;
}
