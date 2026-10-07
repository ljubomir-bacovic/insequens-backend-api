using AutoMapper;
using Insequens.Application.Queries;
using Insequens.Contracts.V1.Tasks;
using Insequens.Domain.Entities;

namespace Insequens.Application.Profiles;

/// <summary>Read projections only. Handlers construct and change entities explicitly.</summary>
public class ToDoItemProfile : Profile
{
    public ToDoItemProfile()
    {
        CreateMap<ToDoItem, ToDoItemGetListModel>()
            .ForCtorParam(nameof(ToDoItemGetListModel.Priority), options => options.MapFrom(item => (TaskPriority?)item.Priority));
        CreateMap<ToDoItem, ToDoItemGetDetailsModel>()
            .ForCtorParam(nameof(ToDoItemGetDetailsModel.Priority), options => options.MapFrom(item => (TaskPriority?)item.Priority));
        CreateMap<ToDoItem, Versioned<ToDoItemGetDetailsModel>>()
            .ForCtorParam(nameof(Versioned<ToDoItemGetDetailsModel>.Value), options => options.MapFrom(item => item))
            .ForCtorParam(nameof(Versioned<ToDoItemGetDetailsModel>.Version), options => options.MapFrom(item => item.RowVersion));
    }
}
