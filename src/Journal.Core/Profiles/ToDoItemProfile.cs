using AutoMapper;
using Journal.Domain.Entities;
using Journal.Domain.Model.ToDoItem;

namespace Journal.Core.Profiles;

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