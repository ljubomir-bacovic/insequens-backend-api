using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Abstractions;
using Insequens.Application.Queries.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Insequens.Contracts.V1.Tasks;
using Insequens.Contracts.V1;

namespace Insequens.Application.Queries.ToDoItem;

public class GetUserToDoItemsHandler(IApplicationDbContext dbContext, IMapper mapper)
    : IRequestHandler<GetUserToDoItemsQuery, PaginatedResult<ToDoItemGetListModel>>
{
    public async Task<PaginatedResult<ToDoItemGetListModel>> Handle(
        GetUserToDoItemsQuery request,
        CancellationToken cancellationToken)
    {
        var criteria = new TaskListCriteria(request.UserId, Completed: request.IsCompleted);
        var query = dbContext.ToDoItems
            .AsNoTracking()
            .Filter(criteria);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Sort(criteria)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<ToDoItemGetListModel>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ToDoItemGetListModel>(items, totalCount, request.Page, request.PageSize);
    }
}
