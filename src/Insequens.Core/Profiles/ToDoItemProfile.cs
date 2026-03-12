using AutoMapper;
using Insequens.Domain.Entities;
using Insequens.Domain.Model.ToDoItem;

namespace Insequens.Core.Profiles;

public class ToDoItemProfile : Profile
{
	public ToDoItemProfile()
	{
        CreateMap<ToDoItemCreateModel, ToDoItem>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()));
        CreateMap<ToDoItem, ToDoItemGetListModel>();
        CreateMap<ToDoItemUpdateModel, ToDoItem>();
        CreateMap<ToDoItem, ToDoItemGetDetailsModel>();
    }
}