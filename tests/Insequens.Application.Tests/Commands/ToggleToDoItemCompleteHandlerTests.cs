using FluentAssertions;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Domain.DataAccess;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Commands;

public class ToggleToDoItemCompleteHandlerTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Handle_WithOwnedItem_TogglesCompletionAndSavesChanges(bool initialValue, bool expectedValue)
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new ToggleToDoItemCompleteCommand(itemId, userId);
        var item = new ToDoItemEntity { Id = itemId, UserId = userId, IsCompleted = initialValue };
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new ToggleToDoItemCompleteHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns(item);

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var result = await handler.Handle(request, cancellationToken);

        result.Should().Be(Unit.Value);
        item.IsCompleted.Should().Be(expectedValue);
        await repository.Received(1).FindAsync(request.ItemId);
        await dataContext.Received(1).SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsToDoItemNotFoundExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new ToggleToDoItemCompleteCommand(itemId, userId);
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
        await dataContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_WithOtherUsersItem_ThrowsResourceForbiddenExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new ToggleToDoItemCompleteCommand(itemId, userId);
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
        await dataContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
