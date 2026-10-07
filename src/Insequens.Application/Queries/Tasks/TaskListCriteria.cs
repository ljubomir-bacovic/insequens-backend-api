using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using DomainPriority = Insequens.Domain.Types.TaskPriority;

namespace Insequens.Application.Queries.Tasks;

/// <summary>What a task list shows and in which order, shared by the v1 and v2 list queries.</summary>
public sealed record TaskListCriteria(
    Guid UserId,
    bool? Completed = null,
    DomainPriority? Priority = null,
    DateOnly? DueFrom = null,
    DateOnly? DueTo = null,
    string? Search = null,
    TaskSortField SortBy = TaskSortField.DueDate,
    SortDirection? SortDirection = null);
