using AutoMapper;
using AutoMapper.QueryableExtensions;
using Insequens.Application.Abstractions;
using Insequens.Contracts.V1;
using Insequens.Contracts.V2.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Queries.Tasks;

public class ListTasksHandler(IApplicationDbContext dbContext, IMapper mapper)
    : IRequestHandler<ListTasksQuery, PaginatedResult<TaskResponse>>
{
    public async Task<PaginatedResult<TaskResponse>> Handle(ListTasksQuery request, CancellationToken cancellationToken)
    {
        var criteria = new TaskListCriteria(
            request.UserId,
            request.Completed,
            (DomainPriority?)request.Priority,
            request.DueFrom,
            request.DueTo,
            request.Search,
            request.SortBy,
            request.SortDirection);
        var query = dbContext.ToDoItems.AsNoTracking().Filter(criteria);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Sort(criteria)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<TaskResponse>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TaskResponse>(items, totalCount, request.Page, request.PageSize);
    }
}
