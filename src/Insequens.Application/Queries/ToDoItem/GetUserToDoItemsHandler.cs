using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Models;
using Insequens.Domain.DataAccess;
using Insequens.Domain.Model.ToDoItem;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Queries.ToDoItem;

public class GetUserToDoItemsHandler(IDataContext dataContext, IMapper mapper)
    : IRequestHandler<GetUserToDoItemsQuery, PaginatedResult<ToDoItemGetListModel>>
{
    public async Task<PaginatedResult<ToDoItemGetListModel>> Handle(
        GetUserToDoItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dataContext.GetRepository<ToDoItemEntity>()
            .AsQueryable()
            .Where(x => x.UserId == request.UserId && x.IsCompleted == request.IsCompleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Priority)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsNoTracking()
            .ProjectTo<ToDoItemGetListModel>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ToDoItemGetListModel>(items, totalCount, request.Page, request.PageSize);
    }
}
