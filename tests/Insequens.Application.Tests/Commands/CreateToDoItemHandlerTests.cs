using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Insequens.Application.Commands.ToDoItem;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Model.ToDoItem;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Tests.Commands;

public class CreateToDoItemHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesItemAndReturnsDetails()
    {
        var userId = Guid.NewGuid();
        var repository = Substitute.For<IRepository<ToDoItemEntity>>();
        var dataContext = Substitute.For<IDataContext>();
        var handler = new CreateToDoItemHandler(dataContext, CreateMapper());
        var request = new CreateToDoItemCommand("Task", "Description", 2, new DateOnly(2026, 1, 1), userId);
        ToDoItemEntity? addedItem = null;

        dataContext.GetRepository<ToDoItemEntity>().Returns(repository);
        repository
            .When(x => x.AddOrUpdate(Arg.Any<ToDoItemEntity>(), Arg.Any<bool?>()))
            .Do(callInfo => addedItem = callInfo.Arg<ToDoItemEntity>());

        var result = await handler.Handle(request, CancellationToken.None);

        addedItem.Should().NotBeNull();
        addedItem!.Id.Should().NotBe(Guid.Empty);
        addedItem.UserId.Should().Be(userId);
        addedItem.Name.Should().Be(request.Name);
        addedItem.Description.Should().Be(request.Description);
        addedItem.DueDate.Should().Be(request.DueDate);
        addedItem.Priority.Should().Be((Domain.Types.TaskPriority?)request.Priority);
        addedItem.IsCompleted.Should().BeFalse();
        result.Should().Be(new ToDoItemGetDetailsModel(
            addedItem.Id,
            request.Name,
            request.Description,
            (Domain.Types.TaskPriority?)request.Priority,
            request.DueDate,
            false));
        repository.Received(1).AddOrUpdate(Arg.Is<ToDoItemEntity>(item =>
            item.Id == addedItem.Id &&
            item.UserId == userId &&
            item.Name == request.Name &&
            item.Description == request.Description &&
            item.DueDate == request.DueDate &&
            item.Priority == (Domain.Types.TaskPriority?)request.Priority &&
            !item.IsCompleted));
        await dataContext.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Send_WithInvalidCommand_ThrowsValidationExceptionBeforeInvokingHandler()
    {
        var dataContext = Substitute.For<IDataContext>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(dataContext);
        services.AddApplication();
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var action = () => mediator.Send(new CreateToDoItemCommand(string.Empty, null, 0, null, Guid.NewGuid()));

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(error =>
            error.PropertyName == "Name" &&
            error.ErrorMessage == "Task name is required.");
        await dataContext.DidNotReceive().SaveChangesAsync();
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
