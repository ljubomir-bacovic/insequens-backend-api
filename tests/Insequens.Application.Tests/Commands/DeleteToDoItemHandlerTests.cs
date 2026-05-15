using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Domain.DataAccess;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Commands;

public class DeleteToDoItemHandlerTests
{
    [Fact]
    public async Task Handle_WithOwnedItem_RemovesItemAndSavesChanges()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new DeleteToDoItemCommand(itemId, userId);
        var item = new ToDoItemEntity { Id = itemId, UserId = userId };
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new DeleteToDoItemHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns(item);

        var result = await handler.Handle(request, CancellationToken.None);

        result.Should().Be(Unit.Value);
        await repository.Received(1).FindAsync(request.ItemId);
        repository.Received(1).Remove(item);
        await dataContext.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsToDoItemNotFoundExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new DeleteToDoItemCommand(itemId, userId);
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var services = new ServiceCollection();

        repository.FindAsync(request.ItemId).Returns((ToDoItemEntity?)null);
        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        services.AddLogging();
        services.AddSingleton(dataContext);
        services.AddApplication();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var action = () => mediator.Send(request);

        var exception = await action.Should().ThrowAsync<ToDoItemNotFoundException>();
        exception.Which.Id.Should().Be(itemId);
        repository.DidNotReceive().Remove(Arg.Any<ToDoItemEntity>());
        await dataContext.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsResourceForbiddenExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new DeleteToDoItemCommand(itemId, userId);
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var services = new ServiceCollection();

        repository.FindAsync(request.ItemId).Returns(new ToDoItemEntity { Id = itemId, UserId = Guid.NewGuid() });
        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        services.AddLogging();
        services.AddSingleton(dataContext);
        services.AddApplication();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var action = () => mediator.Send(request);

        var exception = await action.Should().ThrowAsync<ResourceForbiddenException>();
        exception.Which.Id.Should().Be(itemId);
        repository.DidNotReceive().Remove(Arg.Any<ToDoItemEntity>());
        await dataContext.DidNotReceive().SaveChangesAsync();
    }
}
