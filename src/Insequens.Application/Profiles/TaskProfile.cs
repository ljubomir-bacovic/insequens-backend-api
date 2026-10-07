using AutoMapper;
using Insequens.Application.Queries;
using Insequens.Contracts.V2.Tasks;
using Insequens.Domain.Entities;

namespace Insequens.Application.Profiles;

/// <summary>v2 read projections. The v2 priority shares the domain values.</summary>
public class TaskProfile : Profile
{
    public TaskProfile()
    {
        CreateMap<ToDoItem, TaskResponse>()
            .ForCtorParam(nameof(TaskResponse.Priority), options => options.MapFrom(item => (TaskPriority)item.Priority));
        CreateMap<ToDoItem, Versioned<TaskResponse>>()
            .ForCtorParam(nameof(Versioned<TaskResponse>.Value), options => options.MapFrom(item => item))
            .ForCtorParam(nameof(Versioned<TaskResponse>.Version), options => options.MapFrom(item => item.RowVersion));
    }
}
