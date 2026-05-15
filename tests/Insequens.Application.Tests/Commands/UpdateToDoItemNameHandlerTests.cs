using FluentAssertions;
using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Application.Exceptions;
using Insequens.Domain.DataAccess;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Commands;

public class UpdateToDoItemNameHandlerTests
{
    [Fact]
    public async Task Handle_WithOwnedItem_UpdatesNameAndSavesChanges()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateToDoItemNameCommand(itemId, userId, "Updated task name");
        var item = new ToDoItemEntity { Id = itemId, UserId = userId, Name = "Original task name" };
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new UpdateToDoItemNameHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns(item);

        var result = await handler.Handle(request, CancellationToken.None);

        result.Should().Be(Unit.Value);
        item.Name.Should().Be(request.Name);
        await repository.Received(1).FindAsync(request.ItemId);
        await dataContext.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithMissingItem_ThrowsToDoItemNotFoundException()
    {
        var request = new UpdateToDoItemNameCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated task name");
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new UpdateToDoItemNameHandler(dataContext);

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository.FindAsync(request.ItemId).Returns((ToDoItemEntity?)null);

        var action = () => handler.Handle(request, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ToDoItemNotFoundException>();
        exception.Which.Id.Should().Be(request.ItemId);
        await dataContext.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithEmptyName_ThrowsValidationExceptionBeforeInvokingHandler()
    {
        var request = new UpdateToDoItemNameCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty);
        var dataContext = Substitute.For<IDataContext>();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(dataContext);
        services.AddApplication();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var action = () => mediator.Send(request);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "Name" &&
            error.ErrorMessage == "Task name is required.");
        await dataContext.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithNameLongerThan200Characters_ThrowsValidationExceptionBeforeInvokingHandler()
    {
        var request = new UpdateToDoItemNameCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 201));
        var dataContext = Substitute.For<IDataContext>();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(dataContext);
        services.AddApplication();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var action = () => mediator.Send(request);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "Name" &&
            error.ErrorMessage == "Task name must not exceed 200 characters.");
        await dataContext.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithNonexistentItem_ThrowsToDoItemNotFoundExceptionBeforeInvokingHandler()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var request = new UpdateToDoItemNameCommand(itemId, userId, "Updated task name");
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
        var request = new UpdateToDoItemNameCommand(itemId, userId, "Updated task name");
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
