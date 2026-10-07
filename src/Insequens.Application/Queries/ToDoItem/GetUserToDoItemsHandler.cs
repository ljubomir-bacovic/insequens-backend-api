using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Models;
using Insequens.Application.Abstractions;
using Insequens.Domain.Model.ToDoItem;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Queries.ToDoItem;

public class GetUserToDoItemsHandler(IApplicationDbContext dbContext, IMapper mapper)
    : IRequestHandler<GetUserToDoItemsQuery, PaginatedResult<ToDoItemGetListModel>>
{
    public async Task<PaginatedResult<ToDoItemGetListModel>> Handle(
        GetUserToDoItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ToDoItems
            .AsNoTracking()
            .Where(x => x.UserId == request.UserId && x.IsCompleted == request.IsCompleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Priority)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<ToDoItemGetListModel>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ToDoItemGetListModel>(items, totalCount, request.Page, request.PageSize);
    }
}
