using System.Linq.Expressions;
using Insequens.Contracts.V2;
using Insequens.Contracts.V2.Tasks;
using Microsoft.EntityFrameworkCore;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Queries.Tasks;

internal static class TaskListQueryable
{
    private const string LikeEscape = "\\";

    /// <summary>The caller's tasks matching every given filter. Search is a contains match on name or description.</summary>
    public static IQueryable<ToDoItemEntity> Filter(this IQueryable<ToDoItemEntity> items, TaskListCriteria criteria)
    {
        items = items.Where(item => item.UserId == criteria.UserId);

        if (criteria.Completed is { } completed)
        {
            items = items.Where(item => item.IsCompleted == completed);
        }

        if (criteria.Priority is { } priority)
        {
            items = items.Where(item => item.Priority == priority);
        }

        if (criteria.DueFrom is { } dueFrom)
        {
            items = items.Where(item => item.DueDate >= dueFrom);
        }

        if (criteria.DueTo is { } dueTo)
        {
            items = items.Where(item => item.DueDate <= dueTo);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var pattern = $"%{EscapeLike(criteria.Search.Trim())}%";
            items = items.Where(item =>
                EF.Functions.Like(item.Name, pattern, LikeEscape)
                || (item.Description != null && EF.Functions.Like(item.Description, pattern, LikeEscape)));
        }

        return items;
    }

    /// <summary>
    /// Orders by the chosen field and always last by ID, so pages never overlap or skip a task. A due date sort
    /// puts tasks without one last and breaks ties by priority, most important first.
    /// </summary>
    public static IQueryable<ToDoItemEntity> Sort(this IQueryable<ToDoItemEntity> items, TaskListCriteria criteria)
    {
        var direction = criteria.SortDirection ?? DefaultDirection(criteria.SortBy);

        var ordered = criteria.SortBy switch
        {
            TaskSortField.Priority => items.OrderBy(item => item.Priority, direction),
            TaskSortField.CreatedOn => items.OrderBy(item => item.CreatedOn, direction),
            TaskSortField.Name => items.OrderBy(item => item.Name, direction),
            _ => items
                .OrderBy(item => item.DueDate == null)
                .ThenBy(item => item.DueDate, direction)
                .ThenByDescending(item => item.Priority),
        };

        return ordered.ThenBy(item => item.Id);
    }

    /// <summary>Soonest due, most important, newest and A to Z first unless the client says otherwise.</summary>
    public static SortDirection DefaultDirection(TaskSortField sortBy) => sortBy switch
    {
        TaskSortField.Priority or TaskSortField.CreatedOn => SortDirection.Desc,
        _ => SortDirection.Asc,
    };

    private static IOrderedQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> key,
        SortDirection direction) =>
        direction == SortDirection.Desc ? source.OrderByDescending(key) : source.OrderBy(key);

    private static IOrderedQueryable<T> ThenBy<T, TKey>(
        this IOrderedQueryable<T> source,
        Expression<Func<T, TKey>> key,
        SortDirection direction) =>
        direction == SortDirection.Desc ? source.ThenByDescending(key) : source.ThenBy(key);

    private static string EscapeLike(string text) => text
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_")
        .Replace("[", "\\[");
}
