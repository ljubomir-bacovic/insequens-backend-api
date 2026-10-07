using AutoMapper;
using AutoMapper.Internal;
using FluentAssertions;
using Insequens.Contracts.V1.Tasks;
using Insequens.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Tests.Profiles;

public class ToDoItemProfileTests
{
    [Fact]
    public void Configuration_IsValid()
    {
        CreateMapper().ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Configuration_HasNoMapsOntoTheEntity()
    {
        var typeMaps = CreateMapper().ConfigurationProvider.Internal().GetAllTypeMaps();

        typeMaps.Should().NotContain(typeMap => typeMap.DestinationType == typeof(ToDoItem),
            "handlers construct and change entities explicitly; AutoMapper is for read projections only");
    }

    [Fact]
    public void ToDoItem_MapsToToDoItemGetListModel()
    {
        var entity = NewEntity(DomainPriority.Medium, new DateOnly(2026, 3, 10), isCompleted: true);

        var listModel = CreateMapper().Map<ToDoItemGetListModel>(entity);

        listModel.Should().Be(new ToDoItemGetListModel(
            entity.Id,
            entity.Name,
            entity.Description,
            new DateOnly(2026, 3, 10),
            true,
            TaskPriority.Medium));
    }

    [Fact]
    public void ToDoItem_MapsToToDoItemGetDetailsModel()
    {
        var entity = NewEntity(DomainPriority.High, new DateOnly(2026, 6, 20), isCompleted: false);

        var detailsModel = CreateMapper().Map<ToDoItemGetDetailsModel>(entity);

        detailsModel.Should().Be(new ToDoItemGetDetailsModel(
            entity.Id,
            entity.Name,
            entity.Description,
            TaskPriority.High,
            new DateOnly(2026, 6, 20),
            false));
    }

    [Fact]
    public void ToDoItem_WithoutPriority_MapsToNullPriority()
    {
        var entity = NewEntity(priority: null, dueDate: null, isCompleted: false);

        CreateMapper().Map<ToDoItemGetDetailsModel>(entity).Priority.Should().BeNull();
    }

    private static ToDoItem NewEntity(DomainPriority? priority, DateOnly? dueDate, bool isCompleted) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = "Item",
        Description = "Description",
        Priority = priority,
        DueDate = dueDate,
        IsCompleted = isCompleted,
    };

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
