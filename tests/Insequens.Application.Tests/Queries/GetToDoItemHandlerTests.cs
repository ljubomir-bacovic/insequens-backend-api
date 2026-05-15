using FluentAssertions;
using Insequens.Application;
using Insequens.Application.Exceptions;
using Insequens.Application.Queries.ToDoItem;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Types;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Queries;

public class GetToDoItemHandlerTests
{
    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsToDoItemNotFoundExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new GetToDoItemQuery(itemId, userId);
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
        await repository.Received(1).FindAsync(request.ItemId);
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsResourceForbiddenExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new GetToDoItemQuery(itemId, userId);
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
        await repository.Received(1).FindAsync(request.ItemId);
    }
}
