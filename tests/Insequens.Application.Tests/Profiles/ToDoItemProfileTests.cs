using AutoMapper;
using FluentAssertions;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;
using Insequens.Domain.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application.Tests.Profiles;

public class ToDoItemProfileTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public void ToDoItemCreateModel_MapsToToDoItem_WithNewId()
    {
        var mapper = CreateMapper();
        var model = new ToDoItemCreateModel("Task A", "Description A", (int)TaskPriority.High, new DateOnly(2026, 1, 15));

        var entity = mapper.Map<ToDoItem>(model);

        entity.Id.Should().NotBeEmpty();
        entity.Name.Should().Be(model.Name);
        entity.Description.Should().Be(model.Description);
        entity.Priority.Should().Be((TaskPriority)model.Priority);
        entity.DueDate.Should().Be(model.DueDate);
    }

    [Fact]
    public void ToDoItemCreateModel_MapsToToDoItem_GeneratesUniqueId()
    {
        var mapper = CreateMapper();
        var model = new ToDoItemCreateModel("Task B", null, (int)TaskPriority.Low, null);

        var entity1 = mapper.Map<ToDoItem>(model);
        var entity2 = mapper.Map<ToDoItem>(model);

        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void ToDoItem_MapsToToDoItemGetListModel()
    {
        var mapper = CreateMapper();
        var dueDate = new DateOnly(2026, 3, 10);
        var entity = new ToDoItem
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "List item",
            Description = "List description",
            Priority = TaskPriority.Medium,
            DueDate = dueDate,
            IsCompleted = true,
        };

        var listModel = mapper.Map<ToDoItemGetListModel>(entity);

        listModel.Id.Should().Be(entity.Id);
        listModel.Name.Should().Be(entity.Name);
        listModel.Description.Should().Be(entity.Description);
        listModel.Priority.Should().Be(entity.Priority);
        listModel.DueDate.Should().Be(dueDate);
        listModel.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void ToDoItem_MapsToToDoItemGetDetailsModel()
    {
        var mapper = CreateMapper();
        var dueDate = new DateOnly(2026, 6, 20);
        var entity = new ToDoItem
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Details item",
            Description = null,
            Priority = TaskPriority.High,
            DueDate = dueDate,
            IsCompleted = false,
        };

        var detailsModel = mapper.Map<ToDoItemGetDetailsModel>(entity);

        detailsModel.Id.Should().Be(entity.Id);
        detailsModel.Name.Should().Be(entity.Name);
        detailsModel.Description.Should().BeNull();
        detailsModel.Priority.Should().Be(entity.Priority);
        detailsModel.DueDate.Should().Be(dueDate);
        detailsModel.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void ToDoItemUpdateModel_MapsOntoToDoItem()
    {
        var mapper = CreateMapper();
        var dueDate = new DateOnly(2026, 8, 1);
        var entity = new ToDoItem
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Old name",
            Description = "Old description",
            Priority = TaskPriority.Low,
            DueDate = new DateOnly(2025, 1, 1),
            IsCompleted = false,
        };
        var updateModel = new ToDoItemUpdateModel(entity.Id, "New name", "New description", TaskPriority.High, dueDate, true);

        mapper.Map(updateModel, entity);

        entity.Name.Should().Be(updateModel.Name);
        entity.Description.Should().Be(updateModel.Description);
        entity.Priority.Should().Be(updateModel.Priority);
        entity.DueDate.Should().Be(updateModel.DueDate);
        entity.IsCompleted.Should().BeTrue();
    }
}
