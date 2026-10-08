using Insequens.Contracts.V1;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using MediatR;

namespace Insequens.Application.Queries.Tasks;

/// <param name="SortDirection">Null uses the field's natural order (see <see cref="TaskListQueryable.DefaultDirection"/>).</param>
/// <param name="Deleted">True lists the trash instead of the live tasks.</param>
public record ListTasksQuery(
    Guid UserId,
    bool? Completed = null,
    TaskPriority? Priority = null,
    DateOnly? DueFrom = null,
    DateOnly? DueTo = null,
    string? Search = null,
    TaskSortField SortBy = TaskSortField.DueDate,
    SortDirection? SortDirection = null,
    int Page = 1,
    int PageSize = 20,
    bool Deleted = false) : IRequest<PaginatedResult<TaskResponse>>;
