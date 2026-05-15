using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Domain.DataAccess;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Commands;

public class UpdateToDoItemDescriptionHandlerTests
{
    [Fact]
    public async Task Handle_WithOwnedItem_UpdatesDescriptionAndSavesChanges()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateToDoItemDescriptionCommand(itemId, userId, "Updated task description");
        var item = new ToDoItemEntity { Id = itemId, UserId = userId, Description = "Original task description" };
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new UpdateToDoItemDescriptionHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns(item);

        var result = await handler.Handle(request, CancellationToken.None);

        result.Should().Be(Unit.Value);
        item.Description.Should().Be(request.Description);
        await repository.Received(1).FindAsync(request.ItemId);
        await dataContext.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithNullDescription_ClearsDescriptionAndSavesChanges()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateToDoItemDescriptionCommand(itemId, userId, null);
        var item = new ToDoItemEntity { Id = itemId, UserId = userId, Description = "Original task description" };
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new UpdateToDoItemDescriptionHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns(item);

        await handler.Handle(request, CancellationToken.None);

        item.Description.Should().BeNull();
        await dataContext.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithEmptyDescription_UpdatesDescriptionAndSavesChanges()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateToDoItemDescriptionCommand(itemId, userId, string.Empty);
        var item = new ToDoItemEntity { Id = itemId, UserId = userId, Description = "Original task description" };
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new UpdateToDoItemDescriptionHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns(item);

        await handler.Handle(request, CancellationToken.None);

        item.Description.Should().BeEmpty();
        await dataContext.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithLongDescription_UpdatesDescriptionAndSavesChanges()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var description = new string('a', 10_000);
        var request = new UpdateToDoItemDescriptionCommand(itemId, userId, description);
        var item = new ToDoItemEntity { Id = itemId, UserId = userId, Description = "Original task description" };
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new UpdateToDoItemDescriptionHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns(item);

        await handler.Handle(request, CancellationToken.None);

        item.Description.Should().Be(description);
        await dataContext.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsToDoItemNotFoundExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateToDoItemDescriptionCommand(itemId, userId, "Updated task description");
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
        await dataContext.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsResourceForbiddenExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateToDoItemDescriptionCommand(itemId, userId, "Updated task description");
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
        await dataContext.DidNotReceive().SaveChangesAsync();
    }
}
