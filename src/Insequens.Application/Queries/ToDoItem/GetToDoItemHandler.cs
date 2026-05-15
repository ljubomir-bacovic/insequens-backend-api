using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Model.ToDoItem;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Queries.ToDoItem;

public class GetToDoItemHandler(IDataContext dataContext, IMapper mapper)
    : IRequestHandler<GetToDoItemQuery, ToDoItemGetDetailsModel>
{
    public async Task<ToDoItemGetDetailsModel> Handle(
        GetToDoItemQuery request,
        CancellationToken cancellationToken)
    {
        return await dataContext.GetRepository<ToDoItemEntity>()
            .AsQueryable()
            .Where(item => item.Id == request.ItemId)
            .AsNoTracking()
            .ProjectTo<ToDoItemGetDetailsModel>(mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
