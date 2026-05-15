using AutoMapper;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Model.ToDoItem;
using MediatR;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Queries.ToDoItem;

public class GetToDoItemHandler(IDataContext dataContext, IMapper mapper)
    : IRequestHandler<GetToDoItemQuery, ToDoItemGetDetailsModel>
{
    public async Task<ToDoItemGetDetailsModel> Handle(
        GetToDoItemQuery request,
        CancellationToken cancellationToken)
    {
        var item = (await dataContext.GetRepository<ToDoItemEntity>()
            .FindAsync(request.ItemId))!;

        return mapper.Map<ToDoItemGetDetailsModel>(item);
    }
}
